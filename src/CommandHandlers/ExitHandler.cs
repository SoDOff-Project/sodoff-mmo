using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[CommandHandler(26)]
class ExitHandler : ICommandHandler {
    public void Handle(Client client, NetworkObject receivedObject) {
        client.SetRoom(null);
    }
}
