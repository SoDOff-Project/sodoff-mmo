using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("disableallchats", Role.Moderator)]
class DisableAllChatsCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        Room.DisableAllChatOverrides();
        client.Room.Send(Utils.BuildServerSideMessage("All chat overrides have been disabled", "Server"));
    }
}

