using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[CommandHandler(7)]
class GenericMessageHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        if (!Configuration.ServerConfiguration.EnableChat && receivedObject.Get<string>("m").StartsWith("C:"))
            return Task.CompletedTask;
        NetworkPacket packet = NetworkObject.WrapObject(0, 7, receivedObject).Serialize();
        client.Room.Send(packet);
        return Task.CompletedTask;
    }
}
