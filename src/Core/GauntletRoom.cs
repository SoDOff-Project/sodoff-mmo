using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;
public class GauntletRoom : Room {
    static object NextRoomLock = new object();
    static GauntletRoom? NextRoom = null;

    public static GauntletRoom Get() {
        lock(NextRoomLock) {
            if (NextRoom != null && NextRoom.ClientsCount == 1) {
                var ret = NextRoom!;
                NextRoom = null;
                return ret;
            } else {
                NextRoom = new GauntletRoom();
                return NextRoom!;
            }
        }
    }

    public GauntletRoom() : base (null, "GauntletDO", true) {
        base.RoomVariables.Add(NetworkArray.VlElement("IS_RACE_ROOM", true));
    }

    class Status {
        public string uid;
        public bool isReady = false;
        public string resultA = "";
        public string resultB = "";

        public Status(string uid) {
            this.uid = uid;
        }
    }

    private Dictionary<Client, Status> players = new();

    public void AddPlayer(Client client) {
        players[client] = new Status(client.PlayerData.Uid);
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
        foreach(var player in players) {
            info.Add(player.Value.uid);
            info.Add(player.Value.isReady.ToString());
            info.Add("1"); // TODO this should be player gender
        }
        NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

        Send(packet);
    }

    public void SendPA(Client client) {
        // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["UJR","287997","2","f66cc516-7ea3-40a5-9021-01ff8f290123","false","2","03a3ad99-87a5-4af4-8966-0b2733a05e0f","false","1"]},"r":287997}}
        List<string> info = new();
        info.Add("PA"); // Play Again
        info.Add(base.Id.ToString());
        info.Add("1");
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
                if (player.Value.resultB != "") ++count;
            }
            if (count != 2)
                return false;

            // {"a":13,"c":1,"p":{"c":"msg","p":{"arr":["GC","365587","03a3ad99-87a5-4af4-8966-0b2733a05e0f","10850","79","1","bff0c312-8763-497d-aa0c-a5dfc7d8b861","21050","73","1"]},"r":365587}}
            List<string> info = new();
            info.Add("GC");
            info.Add(base.Id.ToString());
            foreach(var player in players) {
                if (player.Value.resultB == "")
                    continue;
                info.Add(player.Value.uid);
                info.Add(player.Value.resultA);
                info.Add(player.Value.resultB);
                info.Add("1");
            }
            NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "msg", base.Id);

            Send(packet);
            return true;
        }
    }

    static object joinLock = new object();

    static public void Join(Client client, GauntletRoom? room = null) {
        lock(joinLock) {
            if (room is null)
                room = GauntletRoom.Get();

            client.SetRoom(room);
            room.AddPlayer(client); // client will be not removed from GauntletRoom.players ... after remove all client from room whole GauntletRoom.players will be removed
            room.SendUJR();
        }
    }
}
