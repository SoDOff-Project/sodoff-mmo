using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("ban", Role.Moderator)]
class BanCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length < 1) {
            client.Send(Utils.BuildServerSideMessage("Ban: Invalid number of arguments", "Server"));
            return;
        }
        string name = string.Join(' ', arguments);
        Client? target = client.Room.Clients.FirstOrDefault(x => x.PlayerData.DiplayName == name); // Find in this room.
        if (target == null) { // Search all clients in case the offender left the room.
            target = Client.GetAllClients().FirstOrDefault(x => x.PlayerData.DiplayName == name);
            if (target == null) { // If the target is still null, call it a failure.
                client.Send(Utils.BuildServerSideMessage($"Ban: user {name} not found", "Server"));
                return;
            }
        }

        if (target.Banned) {
            client.Send(Utils.BuildServerSideMessage($"Ban: {name} is already banned", "Server"));
        } else {
            target.Ban();
            client.Send(Utils.BuildServerSideMessage($"Ban: {name} has been banned", "Server"));
            if (target.PlayerData.VikingId != null) {
                PunishmentManager.BannedList.Add((int)target.PlayerData.VikingId, target.PlayerData.DiplayName);
                PunishmentManager.SaveMutedBanned();
            } else {
                client.Send(Utils.BuildServerSideMessage($"Ban: This user is not authenticated. They have been merely kicked instead.", "Server"));
            }
        }
    }
}

[ManagementCommand("unban", Role.Moderator)]
class UnbanCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length < 1) {
            client.Send(Utils.BuildServerSideMessage("Unban: Invalid number of arguments", "Server"));
            return;
        }
        string name = string.Join(' ', arguments);
        int? id = null;
        if (PunishmentManager.BannedList.ContainsValue(name)) // Find in list in case they're offline.
            id = PunishmentManager.BannedList.FirstOrDefault(p => p.Value == name).Key;
        
        if (id == null) { // Failure
            client.Send(Utils.BuildServerSideMessage($"Unban: User named \"{name}\" is not banned", "Server"));
            return;
        }

        Client? target = PunishmentManager.BanRoom.Clients.FirstOrDefault(x => x.PlayerData.VikingId == id || x.PlayerData.DiplayName == name); // Find in ban room.
        
        client.Send(Utils.BuildServerSideMessage($"Unban: {name} has been unbanned", "Server"));
        PunishmentManager.BannedList.Remove((int)id);
        PunishmentManager.SaveMutedBanned();
        if (target != null) {
            target.Banned = false;
            target.Disconnect(); // Client will handle relogging.
        }
    }
}