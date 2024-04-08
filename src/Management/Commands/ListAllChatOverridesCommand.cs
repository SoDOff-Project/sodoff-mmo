using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("listallchats", Role.Moderator)]
class ListAllChatOverridesCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        client.Send(Utils.BuildServerSideMessage(string.Join(' ', Room.AllRooms().Where(x => x.AllowChatOverride).Select(x => x.Name)), "Server"));
    }
}

