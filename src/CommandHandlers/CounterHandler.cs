using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("SCE")]
class CounterEventHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) { // {"a":13,"c":1,"p":{"c":"SCE","p":{"NAME":"COUNT"},"r":-1}}
        if (client.Room is SpecialRoom room && room.ambassadorConfig != null) {
            string name = receivedObject.Get<NetworkObject>("p").Get<string>("NAME");
            int index = name switch {
                "COUNT"  => 0,
                "COUNT2" => 1,
                "COUNT3" => 2,
                _ => -1
            };
            if (index != -1) {
                double newVal = room.ambassadorGauges[index] + (1/room.ambassadorConfig.GaugePlayers);
                if (newVal > room.ambassadorConfig.GaugeMax) {
                    room.ambassadorGauges[index] = room.ambassadorConfig.ResetOnMaxReached
                        ? room.ambassadorConfig.GaugeStart
                        : room.ambassadorConfig.GaugeMax;
                } else {
                    room.ambassadorGauges[index] = newVal;
                }
                room.Send(Utils.VlNetworkPacket(room.GetRoomVars(), client.Room.Id));
            } else {
                Console.WriteLine($"Invalid attempt to increment room var {name} in {room.Name}.");
            }
        }
        
        return Task.CompletedTask;
    }
}