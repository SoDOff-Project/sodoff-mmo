using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("announce", Role.Admin)]
class AnnounceCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length == 0) {
            client.Send(Utils.BuildServerSideMessage("Announce: No message to announce", "Server"));
            return;
        }
        client.Room.Send(Utils.BuildServerSideMessage(string.Join(' ', arguments), "Server"));
    }
}
