using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace sodoffmmo.Core;
internal static class Configuration {

    public static ServerConfiguration ServerConfiguration { get; private set; } = new ServerConfiguration();
    public static DiscordBotConfig? DiscordBotConfig { get; private set; } = null;

    public static SortedDictionary<int, object> Emoticons = new();
    public static SortedDictionary<int, object> Actions = new();
    public static SortedDictionary<int, object> EMDCarActions = new();
    public static SortedDictionary<int, object> CannedChat = new();

    public static void Initialize() {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile("emote_data.json", true)
            .Build();

        ServerConfiguration? serverConfiguration = config.GetSection("MMOServer").Get<ServerConfiguration>();
        if (serverConfiguration is null)
            return;

        ServerConfiguration = serverConfiguration;
        if (string.IsNullOrEmpty(ServerConfiguration.ApiUrl)) {
            ServerConfiguration.Authentication = AuthenticationMode.Disabled;
        }

        DiscordBotConfig = config.GetSection("DiscordBot").Get<DiscordBotConfig>();
        if (DiscordBotConfig != null) {
            // Emoticons (the emojis that float above your head)
            SortAndVersion(config.GetSection("Emoticons"), Emoticons);

            // Actions (such as dances)
            SortAndVersion(config.GetSection("Actions"), Actions);

            // In EMD, there are seperate actions when in car that share IDs with normal actions.
            // Since this is easy to check MMO-side (SPM uservar), we can just store these seperate
            SortAndVersion(config.GetSection("EMDCarActions"), EMDCarActions);

            // Canned Chat options (which can vary game to game)
            SortAndVersion(config.GetSection("CannedChat"), CannedChat);
        }
    }

    // While version checking isn't exactly necessary for Actions when using vanilla files, it's nice to support it anyway.
    private static void SortAndVersion(IConfigurationSection section, IDictionary<int, object> dataOut) {
        foreach (IConfigurationSection config in section.GetChildren()) {
            if (config.Value != null) {
                // Consistent for all games
                dataOut.Add(int.Parse(config.Key), config.Value);
            } else {
                // Requires version checks
                OrderedDictionary versioned = new();
                dataOut.Add(int.Parse(config.Key), versioned);
                foreach (IConfigurationSection version in config.GetChildren().OrderByDescending(v=>uint.Parse(v.Key))) {
                    versioned.Add(uint.Parse(version.Key, NumberStyles.AllowHexSpecifier), version.Value);
                }
            }
        }
    }

    public static string? GetVersioned(int id, uint version, IDictionary<int, object> data) {
        object? value = data[id];
        if (value is null or string) {
            return value as string;
        } else if (value is OrderedDictionary versions) {
            foreach (DictionaryEntry ver in versions)
                if (version >= (uint)ver.Key)
                    return (string)ver.Value;
        }
        return null;
    }
}

internal sealed class ServerConfiguration {
    public string ListenIP { get; set; } = string.Empty;
    public int Port { get; set; } = 9933;
    public string EventName { get; set; } = "ScoutAttack";
    public int EventTimer { get; set; } = 30;
    public int FirstEventTimer { get; set; } = 10;
    public Dictionary<string, string[][]> RoomAlerts { get; set; } = new();
    public string[] AmbassadorRooms { get; set; } = Array.Empty<string>();
    public int AmbassadorGaugeStart { get; set; } = 75;
    public float AmbassadorGaugeDecayRate { get; set; } = 60;
    public bool AmbassadorGaugeDecayOnlyWhenInRoom { get; set; } = true;
    public float AmbassadorGaugePlayers { get; set; } = 0.5f;
    public int RacingMaxPlayers { get; set; } = 6;
    public int RacingMinPlayers { get; set; } = 2;
    public int RacingMainLobbyTimer { get; set; } = 15;
    public int PingDelay { get; set; } = 17;
    public bool EnableChat { get; set; } = true;
    public bool AllowChaos { get; set; } = false;
    public AuthenticationMode Authentication { get; set; } = AuthenticationMode.Disabled;
    public string ApiUrl { get; set; } = "";
    public string BypassToken { get; set; } = "";
}

internal sealed class DiscordBotConfig {
    public bool Disabled { get; set; }
    public ulong Server { get; set; }
    public ulong Channel { get; set; }
    public ulong RoleModerator { get; set; }
    public ulong RoleAdmin { get; set; }
    public Dictionary<string, string[]> Merges { get; set; }
}

public enum AuthenticationMode {
    Disabled, Optional, RequiredForChat, Required
}
