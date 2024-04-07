using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("disablechat", Role.Moderator)]
class DisableChatCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        client.Room.AllowChatOverride = false;
        client.Room.Send(Utils.BuildServerSideMessage("Chat has been disabled", "Server"));
    }
}
