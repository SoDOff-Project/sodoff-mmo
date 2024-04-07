using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("tempmute", Role.Moderator)]
class TempMuteCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length != 1) {
            client.Send(Utils.BuildServerSideMessage("TempMute: Invalid number of arguments", "Server"));
            return;
        }
        Client? target = client.Room.Clients.FirstOrDefault(x => x.PlayerData.DiplayName == arguments[0]);
        if (target == null) {
            client.Send(Utils.BuildServerSideMessage($"TempMute: user {arguments[0]} not found", "Server"));
            return;
        }
        target.TempMuted = !target.TempMuted;
        if (target.TempMuted)
            client.Send(Utils.BuildServerSideMessage($"TempMute: {arguments[0]} has been temporarily muted", "Server"));
        else
            client.Send(Utils.BuildServerSideMessage($"TempMute: {arguments[0]} has been unmuted", "Server"));
    }
}
