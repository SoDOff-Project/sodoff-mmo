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
            target = Server.GetAllClients().FirstOrDefault(x => x.PlayerData.DiplayName == name);
            if (target == null) { // Failure
                client.Send(Utils.BuildServerSideMessage($"Mute: user {name} not found", "Server"));
                return;
            }
        }

        if (target.Mute()) {
            client.Send(Utils.BuildServerSideMessage($"Mute: {name} has been muted", "Server"));
        } else {
            client.Send(Utils.BuildServerSideMessage($"Mute: {name} is already muted", "Server"));
        }
        if (target?.PlayerData.VikingId == null) client.Send(Utils.BuildServerSideMessage($"Mute: Their mute will not be persistent since they are not authenticated", "Server"));
    }
}