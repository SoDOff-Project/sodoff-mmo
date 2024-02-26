using System;
using sodoffmmo.Data;

namespace sodoffmmo.Core;
public class Room {
    static int id = 2;
    static Dictionary<string, Room> rooms = new();

    List<Client> clients = new();
    public object roomLock = new object();

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Group { get; private set; }
    public bool IsRaceRoom { get; private set; }

    public Room(string name, string group = null, bool isRaceRoom = false) {
        Id = rooms.Count + 3;
        Name = name;
        if (group is null) {
            Group = name;
        } else {
            Group = group;
        }
        IsRaceRoom = isRaceRoom;
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
            clients.Add(client);
        }
    }

    public void RemoveClient(Client client) {
        lock (roomLock) {
            clients.Remove(client);
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
        roomInfo.Add(true);
        roomInfo.Add(false);
        roomInfo.Add(false);
        if (IsRaceRoom) {
            NetworkArray raceRoom = new();
            raceRoom.Add("IS_RACE_ROOM");
            raceRoom.Add((Byte)1);
            raceRoom.Add(true);
            raceRoom.Add(false);
            raceRoom.Add(false);

            NetworkArray raceRoom2 = new();
            raceRoom2.Add(raceRoom);

            roomInfo.Add((short)1);
            roomInfo.Add((short)2);
            roomInfo.Add(raceRoom2);
        } else {
            roomInfo.Add((short)24);
            roomInfo.Add((short)27);
            roomInfo.Add(new NetworkArray());
        }
        roomInfo.Add((short)0);
        roomInfo.Add((short)0);

        NetworkArray userList = new();
        foreach (Client player in Clients) {
            userList.Add(player.PlayerData.GetNetworkData(player.ClientID));
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
        if (IsRaceRoom) {
            r1.Add((short)1);
            r1.Add((short)2);
        } else {
            r1.Add((short)24);
            r1.Add((short)27);
        }
        r1.Add(new NetworkArray());
        r1.Add((short)0);
        r1.Add((short)0);

        NetworkArray r2 = new();
        r2.Add(Id);
        r2.Add(Name); // Room Name
        r2.Add(Group); // Group Name
        r2.Add(true);
        r2.Add(false);
        r2.Add(false);
        if (IsRaceRoom) {
            r2.Add((short)1);
            r2.Add((short)2);
        } else {
            r2.Add((short)7);
            r2.Add((short)27);
        }
        r2.Add(new NetworkArray());
        r2.Add((short)0);
        r2.Add((short)0);

        list.Add(r1);
        list.Add(r2);

        obj.Add("rl", list);
        obj.Add("g", Name);
        return NetworkObject.WrapObject(0, 15, obj).Serialize();
    }
}
