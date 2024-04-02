using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[CommandHandler(7)]
class GenericMessageHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        NetworkPacket packet = NetworkObject.WrapObject(0, 7, receivedObject).Serialize();
        client.Room.Send(packet);
        return Task.CompletedTask;
    }
}
