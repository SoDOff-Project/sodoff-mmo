using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

using System.Timers;

namespace sodoffmmo.CommandHandlers;


// Host Room For Any
[ExtensionCommandHandler("gs.HRFA")]
class GauntletCreateRoomHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject) {
        GauntletRoom.Join(client);
        return Task.CompletedTask;
    }
}

// Join Any Room
[ExtensionCommandHandler("gs.JAR")]
class GauntletJoinRoomHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject) {
        GauntletRoom.Join(client);
        return Task.CompletedTask;
    }
}

// Play Again
[ExtensionCommandHandler("gs.PA")]
class GauntletPlayAgainHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject) {
        GauntletRoom room = (client.Room as GauntletRoom)!;
        room.SetPlayerReady(client, false);

        NetworkPacket packet = Utils.ArrNetworkPacket(new string[] {
            "LUNR",
            room.Id.ToString(),
            client.PlayerData.Uid
        }, "msg", room.Id);

        room.Send(packet, client);
        room.SendPA(client);
        return Task.CompletedTask;
    }
}

// Lobby User Ready
[ExtensionCommandHandler("gs.LUR")]
class GauntletLobbyUserReadyHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject) {
        GauntletRoom room = (client.Room as GauntletRoom)!;
        room.SetPlayerReady(client);

        NetworkPacket packet = Utils.ArrNetworkPacket(new string[] {
            "LUR",
            room.Id.ToString(),
            client.PlayerData.Uid
        }, "msg", room.Id);

        room.Send(packet);

        if (room.GetReadyCount() > 1) {
            packet = Utils.ArrNetworkPacket(new string[] {
                "LCDD", // Lobby CountDown Done
                room.Id.ToString(),
                client.PlayerData.Uid
            }, "msg", room.Id);
            
            room.Send(packet);
        }
        return Task.CompletedTask;
    }
}

// Lobby User Not Ready
[ExtensionCommandHandler("gs.LUNR")]
class GauntletLobbyUserNotReadyHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject) {
        GauntletRoom room = (client.Room as GauntletRoom)!;
        room.SetPlayerReady(client, false);

        NetworkPacket packet = Utils.ArrNetworkPacket(new string[] {
            "LUNR",
            room.Id.ToString(),
            client.PlayerData.Uid
        }, "msg", room.Id);

        room.Send(packet);
        return Task.CompletedTask;
    }
}

// Game Level Load
[ExtensionCommandHandler("gs.GLL")]
class GauntletLevelLoadHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject) { // {"a":13,"c":1,"p":{"c":"gs.GLL","p":{"0":"0","1":"0","2":"5","en":"GauntletGameExtension"},"r":365587}}
        GauntletRoom room = (client.Room as GauntletRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        
        NetworkPacket packet = Utils.ArrNetworkPacket(new string[] {
            "GLL", // Game CountDown Start
            room.Id.ToString(),
            p.Get<string>("0"),
            p.Get<string>("1"),
            p.Get<string>("2") // TODO use size of p.fields - 1
        }, "msg", room.Id);
        room.Send(packet);
        return Task.CompletedTask;
    }
}

// Game Level Loaded
[ExtensionCommandHandler("gs.GLLD")]
class GauntletLevelLoadedHandler : CommandHandler
{
    private System.Timers.Timer? timer = null;
    private int counter;
    private GauntletRoom room;

    public override Task Handle(Client client, NetworkObject receivedObject) {
        room = (client.Room as GauntletRoom)!;
        counter = 5;
        
        // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["GCDS","365587","4"]},"r":365587}}
        NetworkPacket packet = Utils.ArrNetworkPacket(new string[] {
            "GCDS", // Game CountDown Start
            room.Id.ToString(),
            (--counter).ToString()
        }, "msg", room.Id);
        room.Send(packet);
        
        timer = new System.Timers.Timer(1500);
        timer.AutoReset = true;
        timer.Enabled = true;
        timer.Elapsed += OnTick;
        return Task.CompletedTask;
    }

    private void OnTick(Object? source, ElapsedEventArgs e) {
        NetworkPacket packet;
        if (--counter > 0) {
            // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["GCDS","365587","4"]},"r":365587}}
            packet = Utils.ArrNetworkPacket(new string[] {
                "GCDU", // Game CountDown Update
                room.Id.ToString(),
                counter.ToString()
            }, "msg", room.Id);
        } else {
            // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["GS","365587"]},"r":365587}}
            packet = Utils.ArrNetworkPacket(new string[] {
                "GS", // Game Start
                room.Id.ToString()
            }, "msg", room.Id);
            
            timer!.Stop();
            timer!.Close();
            timer = null;
        }
        room.Send(packet);
    }
}

// Relay Game Data
[ExtensionCommandHandler("gs.RGD")]
class GauntletRelayGameDataHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject) // {"a":13,"c":1,"p":{"c":"gs.RGD","p":{"0":"2700","1":"78","en":"GauntletGameExtension"},"r":4}}
    {
        GauntletRoom room = (client.Room as GauntletRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        
        // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["RGD","365587","150","75"]},"r":365587}}
        NetworkPacket packet = Utils.ArrNetworkPacket(new string[] {
            "RGD", // Relay Game Data
            room.Id.ToString(),
            p.Get<string>("0"),
            p.Get<string>("1")
        }, "msg", room.Id);
        room.Send(packet, client);
        return Task.CompletedTask;
    }
}

// Game Complete
[ExtensionCommandHandler("gs.GC")]
class GauntletGameCompleteHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject) // {"a":13,"c":1,"p":{"c":"gs.GC","p":{"0":"1550","1":"84","en":"GauntletGameExtension"},"r":4}}
    {
        GauntletRoom room = (client.Room as GauntletRoom)!;
        NetworkObject p = receivedObject.Get<NetworkObject>("p");
        
        room.ProcessResult(client, p.Get<string>("0"), p.Get<string>("1"));
        return Task.CompletedTask;
    }
}

