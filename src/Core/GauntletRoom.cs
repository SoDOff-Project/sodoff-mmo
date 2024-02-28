using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;
public class GauntletRoom : Room {
    static List<GauntletRoom> GauntletRooms = new();

    public static GauntletRoom Get() {
        foreach (var room in GauntletRooms) {
            if (room.ClientsCount < 2) {
                lock (room.roomLock) {
                    if (room.ClientsCount == 0)
                        room.players.Clear();
                }
                return room;
            }
        }
        var newroom = new GauntletRoom("GauntletDO" + "_" + GauntletRooms.Count.ToString());
        GauntletRooms.Add(newroom);
        return newroom;
    }

    public GauntletRoom(string name) : base (name, "GauntletDO") {
        base.RoomVariables.Add(NetworkArray.VlElement("IS_RACE_ROOM", true));
    }


    class Status {
        public string uid;
        public bool isReady = false;
        public string resultA = null;
        public string resultB = null;
    }

    private Dictionary<Client, Status> players = new();

    public void AddPlayer(Client client) {
        players[client] = new Status { uid = client.PlayerData.Uid };
    }

    public void SetPlayerReady(Client client, bool status = true) {
        players[client].isReady = status;
    }

    public int GetReadyCount() {
        int count = 0;
        foreach(var player in players) {
            if (player.Value.isReady) ++count;
        }
        return count;
    }

    public void SendUJR() {
        // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["UJR","287997","2","f66cc516-7ea3-40a5-9021-01ff8f290123","false","2","03a3ad99-87a5-4af4-8966-0b2733a05e0f","false","1"]},"r":287997}}
        List<string> info = new();
        info.Add("UJR"); // User Joined Room
        info.Add(base.Id.ToString());
        info.Add("2");
        int i = 0;
        foreach(var player in players) {
            info.Add(player.Value.uid);
            info.Add(player.Value.isReady.ToString());
            info.Add("1"); // TODO this should be player gender
        }
        NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

        foreach(var player in players) {
            player.Key.Send(packet);
        }
    }

    public void SendPA(Client client) {
        // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["UJR","287997","2","f66cc516-7ea3-40a5-9021-01ff8f290123","false","2","03a3ad99-87a5-4af4-8966-0b2733a05e0f","false","1"]},"r":287997}}
        List<string> info = new();
        info.Add("PA"); // Play Again
        info.Add(base.Id.ToString());
        info.Add("1");
        int i = 0;
        foreach(var player in players) {
            info.Add(player.Value.uid);
            info.Add(player.Value.isReady.ToString());
            info.Add("1"); // TODO this should be player gender
        }
        NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

        client.Send(packet);
    }

    public bool ProcessResult(Client client, string resultA, string resultB) {
        lock (base.roomLock) {
            players[client].resultA = resultA;
            players[client].resultB = resultB;

            int count = 0;
            foreach(var player in players) {
                if (player.Value.resultA != null) ++count;
            }
            if (count != 2)
                return false;

            // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["GC","365587","03a3ad99-87a5-4af4-8966-0b2733a05e0f","10850","79","1","bff0c312-8763-497d-aa0c-a5dfc7d8b861","21050","73","1"]},"r":365587}}
            List<string> info = new();
            info.Add("GC");
            info.Add(base.Id.ToString());
            foreach(var player in players) {
                if (player.Value.resultA is null)
                    continue;
                info.Add(player.Value.uid);
                info.Add(player.Value.resultA);
                info.Add(player.Value.resultB);
                info.Add("1");
            }
            NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

            foreach(var player in players) {
                player.Key.Send(packet);
            }

            return true;
        }
    }
    
    static public void Join(Client client, GauntletRoom room = null) {
        if (room is null)
            room = GauntletRoom.Get();

        room.AddPlayer(client); // must be call before InvalidatePlayerData - we need uid

        client.LeaveRoom();
        client.InvalidatePlayerData();

        client.Send(room.RespondJoinRoom());
        client.Send(room.SubscribeRoom());
        room.SendUJR();

        client.Room = room;
    }
}
