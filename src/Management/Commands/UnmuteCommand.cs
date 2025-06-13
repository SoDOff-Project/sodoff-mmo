using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

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
            target = Server.GetAllClients().FirstOrDefault(x => x.PlayerData.DiplayName == name);
            if (target == null && id == null) { // Failure
                client.Send(Utils.BuildServerSideMessage($"Unmute: user {name} not found", "Server"));
                return;
            }
        }

        if (target.Unmute(id)) {
            client.Send(Utils.BuildServerSideMessage($"Unmute: {name} has been unmuted", "Server"));
        } else {
            client.Send(Utils.BuildServerSideMessage($"Unmute: {name} is not muted", "Server"));
        }
    }
}
