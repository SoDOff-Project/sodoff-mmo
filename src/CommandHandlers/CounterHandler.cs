using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("SCE")]
class CounterEventHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) { // {"a":13,"c":1,"p":{"c":"SCE","p":{"NAME":"COUNT"},"r":-1}}
        if (client.Room is AmbassadorRoom room) {
            string name = receivedObject.Get<NetworkObject>("p").Get<string>("NAME");
            if (name == "COUNT" || name == "COUNT2" || name == "COUNT3") {
                int index = name switch {
                    "COUNT"  => 0,
                    "COUNT2" => 1,
                    "COUNT3" => 2
                };
                room.ambassadorGauges[index] = Math.Min(100, room.ambassadorGauges[index]+(1/Configuration.ServerConfiguration.AmbassadorGaugePlayers));
                NetworkArray vars = new();
                room.AddRoomData(vars);
                room.Send(Utils.VlNetworkPacket(vars, client.Room.Id));
            } else {
                Console.WriteLine($"Invalid attempt to increment room var {name} in {room.Name}.");
            }
        }
        
        return Task.CompletedTask;
    }
}