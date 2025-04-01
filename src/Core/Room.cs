using System;
using sodoffmmo.Data;
using System.Timers;

namespace sodoffmmo.Core;
public class Room {
    public static int MaxId { get; private set; } = 2;
    static object RoomsListLock = new object();
    protected static Dictionary<string, Room> rooms = new();

    List<Client> clients = new();
    protected object roomLock = new object();

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Group { get; private set; }
    public bool AutoRemove { get; private set; }
    public bool IsRemoved { get; private set; } = false;

    public bool AllowChatOverride { get; set; } = false;
    public NetworkArray RoomVariables = new();

    public Room(string? name, string? group = null, bool autoRemove = false) {
        Id = ++MaxId;
        if (name is null) {
            Name = group! + "_" + MaxId;
        } else {
            Name = name;
        }
        if (group is null) {
            Group = name!;
        } else {
            Group = group;
        }
        AutoRemove = autoRemove;
        rooms.Add(Name, this);

        if (Configuration.ServerConfiguration.MakeChaos)
            SetTimer(2);
    }

    public int ClientsCount => clients.Count;

    public IEnumerable<Client> Clients {
        get {
            List<Client> list;
            lock (roomLock) {
                list = new List<Client>(clients);
            }
            return list;
        }
    }

    public void AddClient(Client client) {
        lock (roomLock) {
            if (IsRemoved)
                throw new Exception("Call AddClient on removed room");
            client.Send(RespondJoinRoom());
            // NOTE: send RespondJoinRoom() and add client to clients as atomic operation
            //       to make sure to client get full list of players in room
            clients.Add(client);
        }
    }

    public void RemoveClient(Client client) {
        lock (roomLock) {
            clients.Remove(client);
            if (AutoRemove && ClientsCount == 0) {
                IsRemoved = true;
                rooms.Remove(Name);
            }
        }
    }

    public void Send(NetworkPacket packet, Client? skip = null) {
        foreach (var roomClient in Clients) {
            if (roomClient != skip) {
                roomClient.Send(packet);
            }
        }
    }

    public static bool Exists(string name) => rooms.ContainsKey(name);

    public static Room Get(string name) => rooms[name];
    
    public static Room GetOrAdd(string name, bool autoRemove = false) {
        lock(RoomsListLock) {
            if (!Room.Exists(name)) 
                return new Room(name, autoRemove: autoRemove);
            return rooms[name];
        }
    }

    public static Room[] AllRooms() {
        return rooms.Values.ToArray();
    }

    public static void DisableAllChatOverrides() {
        lock (RoomsListLock) {
            foreach (var room in rooms) {
                room.Value.AllowChatOverride = false;
            }
        }
    }

    public NetworkPacket RespondJoinRoom() {
        NetworkObject obj = new();
        NetworkArray roomInfo = new();
        roomInfo.Add(Id);
        roomInfo.Add(Name); // Room Name
        roomInfo.Add(Group); // Group Name
        roomInfo.Add(true); // is game
        roomInfo.Add(false); // is hidden
        roomInfo.Add(false); // is password protected
        roomInfo.Add((short)clients.Count); // player count
        roomInfo.Add((short)4096); // max player count
        roomInfo.Add(GetRoomVars()); // variables (plus added data)
        roomInfo.Add((short)0); // spectator count
        roomInfo.Add((short)0); // max spectator count

        NetworkArray userList = new();
        foreach (Client player in clients) {
            if (player.PlayerData.Uid != "")
                userList.Add(player.PlayerData.GetNetworkData(player.ClientID, out _));
        }

        obj.Add("r", roomInfo);
        obj.Add("ul", userList);

        NetworkPacket packet = NetworkObject.WrapObject(0, 4, obj).Serialize();
        packet.Compress();

        return packet;
    }

    public NetworkPacket SubscribeRoom() {
        NetworkObject obj = new();
        NetworkArray list = new();

        NetworkArray r1 = new();
        r1.Add(Id);
        r1.Add(Name); // Room Name
        r1.Add(Group); // Group Name
        r1.Add(true);
        r1.Add(false);
        r1.Add(false);
        r1.Add((short)clients.Count); // player count
        r1.Add((short)4096); // max player count
        r1.Add(GetRoomVars());
        r1.Add((short)0);
        r1.Add((short)0);

        list.Add(r1);

        obj.Add("rl", list);
        obj.Add("g", Group);
        return NetworkObject.WrapObject(0, 15, obj).Serialize();
    }

    internal virtual NetworkArray GetRoomVars() { return RoomVariables; }


    private System.Timers.Timer timer;
    private Random random = new Random();

    private void SetTimer(double timeout) {
        if (timer != null) {
            timer.Stop();
            timer.Close();
        }

        timer = new System.Timers.Timer(timeout * 1000);
        timer.AutoReset = false;
        timer.Enabled = true;
        timer.Elapsed += OnTimer;
    }

    private void OnTimer(Object? source, ElapsedEventArgs e) {
        SetTimer(random.NextDouble() * 9 + 1);
        foreach (var roomClient in Clients) {
            roomClient.PlayerData.RandomizeDragon();

            NetworkArray vl = new();
            NetworkObject data = new();
            var value = roomClient.PlayerData.GetVariable("FP");
            data.Add("FP", value);
            vl.Add(NetworkArray.Param("FP", value));

            roomClient.SendSUV(vl, data);
        }
    }
}
