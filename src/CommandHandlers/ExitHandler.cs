using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[CommandHandler(26)]
class ExitHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        client.SetRoom(null);
        return Task.CompletedTask;
    }
}
