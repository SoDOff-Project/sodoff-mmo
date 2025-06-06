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
        Client? target = client.Room.Clients.FirstOrDefault(x => x.PlayerData.DiplayName == name);
        if (target == null) {
            client.Send(Utils.BuildServerSideMessage($"TempMute: user {name} not found", "Server"));
            return;
        }

        if (target.RealMuted) {
            client.Send(Utils.BuildServerSideMessage($"TempMute: {name} is already regular muted", "Server"));
        } else {
            target.TempMuted = !target.TempMuted;
            if (target.TempMuted)
                client.Send(Utils.BuildServerSideMessage($"TempMute: {name} has been temporarily muted", "Server"));
            else
                client.Send(Utils.BuildServerSideMessage($"TempMute: {name} has been unmuted", "Server"));
        }
    }
}
