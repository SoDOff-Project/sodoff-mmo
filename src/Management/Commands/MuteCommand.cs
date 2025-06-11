using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("mute", Role.Moderator)]
class MuteCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length < 1) {
            client.Send(Utils.BuildServerSideMessage("Mute: Invalid number of arguments", "Server"));
            return;
        }
        string name = string.Join(' ', arguments);
        Client? target = client.Room.Clients.FirstOrDefault(x => x.PlayerData.DiplayName == name); // Find in this room.
        if (target == null) { // Search all clients in case the offender left the room.
            target = Client.GetAllClients().FirstOrDefault(x => x.PlayerData.DiplayName == name);
            if (target == null) { // Failure
                client.Send(Utils.BuildServerSideMessage($"Mute: user {name} not found", "Server"));
                return;
            }
        }

        if (target.PlayerData.VikingId != null) {
            if (PunishmentManager.MutedList.ContainsKey((int)target.PlayerData.VikingId)) {
                client.Send(Utils.BuildServerSideMessage($"Mute: {name} is already muted", "Server"));
            } else {
                target.Muted = true;
                client.Send(Utils.BuildServerSideMessage($"Mute: {name} has been muted", "Server"));
                PunishmentManager.MutedList.Add((int)target.PlayerData.VikingId, target.PlayerData.DiplayName);
                PunishmentManager.SaveMutedBanned();
            }
        } else {
            target.Muted = true;
            client.Send(Utils.BuildServerSideMessage($"Mute: {name} has been muted", "Server"));
            client.Send(Utils.BuildServerSideMessage($"Mute: This user is not authenticated. Their mute will not be permanent!", "Server"));
        }
    }
}

[ManagementCommand("unmute", Role.Moderator)]
class UnmuteCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length < 1) {
            client.Send(Utils.BuildServerSideMessage("Unmute: Invalid number of arguments", "Server"));
            return;
        }
        string name = string.Join(' ', arguments);
        int? id = null;
        if (PunishmentManager.MutedList.ContainsValue(name)) id = PunishmentManager.MutedList.FirstOrDefault(p => p.Value == name).Key; // Find in list in case they're offline.
        Client? target = client.Room.Clients.FirstOrDefault(x => x.PlayerData.VikingId == id || x.PlayerData.DiplayName == name); // Find in this room.
        if (target == null) { // Search all clients in case the target left the room.
            target = Client.GetAllClients().FirstOrDefault(x => x.PlayerData.DiplayName == name);
            if (target == null && id == null) { // Failure
                client.Send(Utils.BuildServerSideMessage($"Unmute: user {name} not found", "Server"));
                return;
            }
        }

        if (target?.Muted == false || (id != null && !PunishmentManager.MutedList.ContainsKey((int)id))) {
            client.Send(Utils.BuildServerSideMessage($"Unmute: {name} is not muted", "Server"));
        } else {
            if (target != null) {
                target.Muted = false;
            }
            client.Send(Utils.BuildServerSideMessage($"Unmute: {name} has been unmuted", "Server"));
            if (id != null) {
                PunishmentManager.MutedList.Remove((int)id);
                PunishmentManager.SaveMutedBanned();
            }
        }
    }
}