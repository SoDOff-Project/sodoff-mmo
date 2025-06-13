using sodoffmmo.Core;
using System.Text.Json;

namespace sodoffmmo.Management;

public static class PunishmentManager {
    // VikingId, Reason (nullable)
    public static readonly Dictionary<int, string?> MutedList;
    public static readonly Dictionary<int, string?> BannedList;

    public static readonly BanRoom BanRoom = new("YouAreBanned");

    static PunishmentManager() { // Runs when Client class is loaded
        if (File.Exists("muted_banned_users.json")) {
            try {
                MutedBannedData? data = JsonSerializer.Deserialize<MutedBannedData>(File.ReadAllText("muted_banned_users.json"));
                if (data != null) {
                    MutedList = data.MutedList ?? new();
                    BannedList = data.BannedList ?? new();
                    return;
                }
            } catch {
                Console.WriteLine("Data for Muted/Banned users wasn't read. It might be corrupted or malformed.");
            }
        }
        MutedList = new();
        BannedList = new();
    }
    public static void SaveMutedBanned() {
        File.WriteAllText("muted_banned_users.json", JsonSerializer.Serialize(new MutedBannedData {
            MutedList = MutedList,
            BannedList = BannedList
        }, new JsonSerializerOptions {
             WriteIndented = true
        }));
    }

    /// <summary>
    /// Mute a user.
    /// </summary>
    /// <param name="target">User to mute - can be null, but id should be set in that case.</param>
    /// <param name="id">Optional vikingid of the user to mute.</param>
    /// <returns>False if already muted, otherwise true.</returns>
    public static bool Mute(this Client? target, int? id = null) {
        if (target != null) {
            id ??= target.PlayerData.VikingId;
            if (target.Muted && id == null) return false;
            target.Muted = true;
        }

        if (id != null) {
            if (MutedList.ContainsKey((int)id)) return false;
            MutedList.Add((int)id, target?.PlayerData.DiplayName);
            SaveMutedBanned();
        }

        return true;
    }

    /// <summary>
    /// Unmute a user.
    /// </summary>
    /// <param name="target">User to unmute - can be null, but id should be set in that case.</param>
    /// <param name="id">Optional vikingid of the user to unmute.</param>
    /// <returns>False if the user isn't muted, otherwise true.</returns>
    public static bool Unmute(this Client? target, int? id=null) {
        if (target != null) {
            if (!target.Muted) return false;
            target.Muted = false;
            id ??= target.PlayerData.VikingId;
        }

        if (id != null) {
            if (!MutedList.Remove((int)id)) return false;
            SaveMutedBanned();
        }

        return true;
    }

    /// <summary>
    /// Ban a user.
    /// </summary>
    /// <param name="target">User to ban - can be null, but id should be set in that case.</param>
    /// <param name="id">Optional vikingid of the user to ban.</param>
    /// <returns>False if already banned, otherwise true.</returns>
    public static bool Ban(this Client? target, int? id=null) {
        if (target != null) {
            if (target.Banned) return false;
            target.Banned = true;
            target.SetRoom(BanRoom);
            id ??= target.PlayerData.VikingId;
        }

        if (id != null) {
            if (BannedList.ContainsKey((int)id)) return false;
            BannedList.Add((int)id, target?.PlayerData.DiplayName);
            SaveMutedBanned();
        }

        return true;
    }

    /// <summary>
    /// Unban a user.
    /// </summary>
    /// <param name="target">User to unban - can be null, but id should be set in that case.</param>
    /// <param name="id">Optional vikingid of the user to unban.</param>
    /// <returns>False if the user isn't banned, otherwise true.</returns>
    public static bool Unban(this Client? target, int? id=null) {
        if (target != null) {
            if (!target.Banned) return false;
            target.Banned = false;
            target.Disconnect(); // Client will handle relogging.
            id ??= target.PlayerData.VikingId;
        }

        if (id != null) {
            if (!BannedList.Remove((int)id)) return false;
            SaveMutedBanned();
        }

        return true;
    }
}

internal sealed class MutedBannedData {
    public Dictionary<int, string?>? MutedList { get; set; }
    public Dictionary<int, string?>? BannedList { get; set; }
}