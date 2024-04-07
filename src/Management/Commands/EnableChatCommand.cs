using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("enablechat", Role.Moderator)]
class EnableChatCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        client.Room.AllowChatOverride = true;
        client.Room.Send(Utils.BuildServerSideMessage("Chat has been enabled", "Server"));
    }
}
