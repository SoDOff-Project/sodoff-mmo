using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;
public class Room {
    public static int MaxId { get; private set; } = 2;
    protected static Dictionary<string, Room> rooms = new();

    List<Client> clients = new();
    protected object roomLock = new object();

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Group { get; private set; }
    public bool AutoRemove { get; private set; }
    public bool IsRemoved { get; private set; } = false;
    public NetworkArray RoomVariables = new();

    public Room(string name, string group = null, bool autoRemove = false) {
        Id = ++MaxId;
        if (name is null) {
            Name = group + "_" + MaxId;
        } else {
            Name = name;
        }
        if (group is null) {
            Group = name;
        } else {
            Group = group;
        }
        AutoRemove = autoRemove;
        rooms.Add(Name, this);
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

    public static bool Exists(string name) => rooms.ContainsKey(name);

    public static Room Get(string name) => rooms[name];
    
    public static Room GetOrAdd(string name) {
        if (!Room.Exists(name))
            return new Room(name);
        return rooms[name];
    }

    public static Room[] AllRooms() {
        return rooms.Values.ToArray();
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
        roomInfo.Add(RoomVariables); // variables
        roomInfo.Add((short)0); // spectator count
        roomInfo.Add((short)0); // max spectator count

        NetworkArray userList = new();
        foreach (Client player in Clients) {
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
        r1.Add(new NetworkArray());
        r1.Add((short)0);
        r1.Add((short)0);

        list.Add(r1);

        obj.Add("rl", list);
        obj.Add("g", Group);
        return NetworkObject.WrapObject(0, 15, obj).Serialize();
    }
}
