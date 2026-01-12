using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("tempmute", Role.Moderator)]
class TempMuteCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length < 1) {
            client.Send(Utils.BuildServerSideMessage("TempMute: Invalid number of arguments", "Server"));
            return;
        }
        string name = string.Join(' ', arguments);
        var targets = client.Room.Clients.Where(x => x.PlayerData.DisplayName == name);
        // NOTE: using DisplayName here because this is the name displayed in client-side chat history
        if (targets.Count() == 0) {
            client.Send(Utils.BuildServerSideMessage($"TempMute: user {name} not found", "Server"));
            return;
        } else if (targets.Count() > 1) {
            client.Send(Utils.BuildServerSideMessage($"TempMute: found multiple users with the name: {name}", "Server"));
            return;
        }

        Client target = targets.First();
        if (target.Muted) {
            client.Send(Utils.BuildServerSideMessage($"TempMute: {name} is already muted", "Server"));
        } else {
            target.Muted = true;
            client.Send(Utils.BuildServerSideMessage($"TempMute: {name} has been temporarily muted", "Server"));
        }
    }
}
