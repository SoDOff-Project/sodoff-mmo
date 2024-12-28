using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("SCE")]
class CounterEventHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) { // {"a":13,"c":1,"p":{"c":"SCE","p":{"NAME":"COUNT"},"r":-1}}
        if (client.Room?.HasAmbassador() is bool b && b) {
            string name = receivedObject.Get<NetworkObject>("p").Get<string>("NAME");
            if (name == "COUNT" || name == "COUNT2" || name == "COUNT3") {
                int index = name switch {
                    "COUNT"  => 0,
                    "COUNT2" => 1,
                    "COUNT3" => 2
                };
                client.Room.ambassadorGauges[index] = Math.Min(100, client.Room.ambassadorGauges[index]+(1/Configuration.ServerConfiguration.AmbassadorGaugePlayers));
                client.Room.Send(Utils.VlNetworkPacket(client.Room.AddAmbassadorData(new()), client.Room.Id));
            } else {
                Console.WriteLine("Invalid attempt to increment room var "+name+".");
            }
        }
        
        return Task.CompletedTask;
    }
}