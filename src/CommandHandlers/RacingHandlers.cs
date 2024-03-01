using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

using System.Timers;

namespace sodoffmmo.CommandHandlers;

// Set Player Ready
[ExtensionCommandHandler("dr.PR")]
class RacingPlayerReadyHandler : ICommandHandler
{
    public void Handle(Client client, NetworkObject receivedObject) { // {"a":13,"c":1,"p":{"c":"dr.PR","p":{"IMR":"True","en":""},"r":-1}}
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        RacingPlayerState ready = p.Get<string>("IMR") == "True" ? RacingPlayerState.Ready : RacingPlayerState.NotReady;
        
        // client send also {"a":7,"c":0,"p":{"m":"IMR:ae70ef16-c52c-43e4-9305-d6ea3e378a0d:True","r":412456,"t":0,"u":3529456}}
        // so server do not need generate this packet
        
        if (client.Room.Group == "RacingDragon") {
            RacingRoom room = client.Room as RacingRoom;
            room.SetPlayerState(client, ready);
            Console.WriteLine($"IMR Lobby: {client.ClientID} {ready}");
            room.TryLoad();
        } else {
            RacingLobby.SetPlayerState(client, ready);
            Console.WriteLine($"IMR: {client.ClientID} {ready}");
        }
    }
}

// Player Status Request
[ExtensionCommandHandler("dr.PS")]
class RacingPlayerStatusHandler : ICommandHandler
{
    public void Handle(Client client, NetworkObject receivedObject) {
        RacingLobby.SendToRacingRoom();
        client.Send(RacingLobby.GetPS());
    }
}

// User Ready ACK
[ExtensionCommandHandler("dr.UACK")]
class RacingUACKHandler : ICommandHandler
{
    public void Handle(Client client, NetworkObject receivedObject) {
        RacingRoom room = client.Room as RacingRoom;
        room.SetPlayerState(client, RacingPlayerState.RaceReady1);
    }
}

// All Ready ACK
[ExtensionCommandHandler("dr.ARACK")]
class RacingARACKHandler : ICommandHandler
{
    public void Handle(Client client, NetworkObject receivedObject) {
        RacingRoom room = client.Room as RacingRoom;
        room.SetPlayerState(client, RacingPlayerState.RaceReady2);

        if (room.GetPlayersCount(RacingPlayerState.RaceReady2) == room.ClientsCount) {
            NetworkPacket packet = room.GetSTAPacket();
            foreach (var roomClient in room.Clients) {
                roomClient.Send(packet);
            }
            Console.WriteLine($"STA");
        }
    }
}

[ExtensionCommandHandler("dr.AR")]
class RacingARHandler : ICommandHandler
{
    public void Handle(Client client, NetworkObject receivedObject) { // {"a":13,"c":1,"p":{"c":"dr.AR","p":{"CT":"112.1268","FD":"3008.283","LC":"3","UN":"scourgexxwulf","en":""},"r":412467}}
        RacingRoom room = client.Room as RacingRoom;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        
        room.SetResults(client, p.Get<string>("UN"), p.Get<string>("CT"), p.Get<string>("LC"));
        room.SendResults();
    }
}
