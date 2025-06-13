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
            target = Server.GetAllClients().FirstOrDefault(x => x.PlayerData.DiplayName == name);
            if (target == null) { // If the target is still null, call it a failure.
                client.Send(Utils.BuildServerSideMessage($"Ban: user {name} not found", "Server"));
                return;
            }
        }

        if (target.Ban()) {
            if (target?.PlayerData.VikingId == null) {
                client.Send(Utils.BuildServerSideMessage($"Ban: {name} has been banned", "Server"));
            } else {
                client.Send(Utils.BuildServerSideMessage($"Ban: {name}'s session has been banned (they are not authenticated)", "Server"));
            }
        } else {
            client.Send(Utils.BuildServerSideMessage($"Ban: {name} is already banned", "Server"));
        }
    }
}