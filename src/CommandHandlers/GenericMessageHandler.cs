using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[CommandHandler(7)]
class GenericMessageHandler : CommandHandler {
    private static readonly string[] AllowedObjectMessages = [
        "SO:PfCatwalkManager:OnQS" // Used by SuperSecret
    ];
    
    public override Task Handle(Client client, NetworkObject receivedObject) {
        string message = receivedObject.Get<string>("m");
        if ((
            !Configuration.ServerConfiguration.EnableCannedChat &&
            message.StartsWith("C:")
        ) || (
            !Configuration.ServerConfiguration.AllowChaos &&
            message.StartsWith("SO:") &&
            !AllowedObjectMessages.Contains(message)
        )) return Task.CompletedTask;
        
        NetworkPacket packet = NetworkObject.WrapObject(0, 7, receivedObject).Serialize();
        client.Room.Send(packet);
        return Task.CompletedTask;
    }
}
