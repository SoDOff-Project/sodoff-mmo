using System;
using sodoffmmo.Data;

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
        roomInfo.Add(RoomVariables); // variables
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
        r1.Add(new NetworkArray());
        r1.Add((short)0);
        r1.Add((short)0);

        list.Add(r1);

        obj.Add("rl", list);
        obj.Add("g", Group);
        return NetworkObject.WrapObject(0, 15, obj).Serialize();
    }


    private int alertId = -1;
    private Random random = new Random();

    List<AlertInfo> alerts = new();

    public void AddAlert(AlertInfo alert) {
        alerts.Add(alert);
        ResetAlertTimer(alert);
    }

    public void SendAllAlerts(Client client) {
        foreach (AlertInfo alert in alerts) {
            if (alert.IsRunning()) StartAlert(alert, client);
        }
    }


    private void StartAlert(AlertInfo alert, Client? specificClient = null) {
        alert.songId = random.Next(0, alert.songs);
        NetworkArray NewRoomVariables = new();
        NewRoomVariables.Add(NetworkArray.VlElement(REDALERT_START, alertId++, isPersistent: true));
        NewRoomVariables.Add(NetworkArray.VlElement(REDALERT_TYPE, alert.type, isPersistent: true));
        double duration = (alert.endTime - DateTime.Now).TotalSeconds;
        NewRoomVariables.Add(NetworkArray.VlElement(REDALERT_LENGTH, alert.type == "1" ? alert.redAlertDuration : duration, isPersistent: true));
        if (alert.type == "1") NewRoomVariables.Add(NetworkArray.VlElement(REDALERT_TIMEOUT, duration, isPersistent: true));
        if (alert.type == "3") NewRoomVariables.Add(NetworkArray.VlElement(REDALERT_SONG, (double)alert.songId, isPersistent: true));
        NetworkPacket packet = Utils.VlNetworkPacket(NewRoomVariables, Id);
        if (specificClient is null) {
            RoomVariables = NewRoomVariables;
            Send(packet);
            Console.WriteLine("Started event " +alert + " in room " + Name);
        } else {
            specificClient.Send(packet);
            Console.WriteLine("Added " + specificClient.PlayerData.DiplayName + " to event " + alert + " with " + duration + " seconds remaining");
        }
    }

    void ResetAlertTimer(AlertInfo alert) {
        System.Timers.Timer? timer = alert.timer;
        if (timer != null) {
            timer.Stop();
            timer.Close();
        }
        DateTime startTime = DateTime.Now.AddMilliseconds(random.Next(alert.minTime * 1000, alert.maxTime * 1000));
        DateTime endTime = startTime.AddSeconds(alert.duration);
        for (int i = 0; i < alerts.IndexOf(alert); i++) {
            // Prevent overlap between two events.
            if (alerts[i].Overlaps(endTime)) {
                startTime = alerts[i].endTime.AddSeconds(5);
                endTime = startTime.AddSeconds(alert.duration);
            }
        }
        timer = new System.Timers.Timer((startTime - DateTime.Now).TotalMilliseconds);
        timer.AutoReset = false;
        timer.Enabled = true;
        timer.Elapsed += (sender, e) => StartAlert(alert);
        timer.Elapsed += (sender, e) => ResetAlertTimer(alert);
        alert.timer = timer;
        Console.WriteLine("Event " + alert + " in " + Name + " scheduled for " + startTime.ToString("MM/dd/yyyy HH:mm:ss tt") + " (in " + (startTime - DateTime.Now).TotalSeconds + " seconds)");
        alert.startTime = startTime;
        alert.endTime = endTime;
    }

    private const string REDALERT_START = "RA_S";
    private const string REDALERT_TYPE = "RA_A";
    private const string REDALERT_LENGTH = "RA_L";
    private const string REDALERT_TIMEOUT = "RA_T";
    private const string REDALERT_SONG = "RA_SO";

    public class AlertInfo {
        public readonly string type;
        public readonly double duration;
        public readonly int minTime;
        public readonly int maxTime;
        public readonly int redAlertDuration;
        public readonly int songs;
        public int songId;

        public DateTime startTime {
            get {
                return newStartTime;
            }
            set {
                oldStartTime = newStartTime;
                newStartTime = value;
            }
        }

        public DateTime endTime {
            get {
                return newEndTime;
            }
            set {
                oldEndTime = newEndTime;
                newEndTime = value;
            }
        }

        private DateTime newStartTime;
        private DateTime newEndTime;
        private DateTime oldStartTime;
        private DateTime oldEndTime;

        public System.Timers.Timer? timer = null;

        public AlertInfo(string type, double duration = 20.0, int minTime = 30, int maxTime = 240, int redAlertDuration = 60, int songs = 16) {
            this.type = type;
            this.duration = duration;
            this.minTime = minTime;
            this.maxTime = maxTime;
            this.redAlertDuration = redAlertDuration;
            this.songs = songs;
        }

        public bool Overlaps(DateTime time) {
            return (time >= oldStartTime && time <= oldEndTime);
        }

        public bool IsRunning() {
            return Overlaps(DateTime.Now);
        }

        public override string ToString() {
            return type switch {
                "1" => "RedAlert",
                "2" => "DiscoAlert",
                "3" => "DanceOff",
                _ => type
            };
        }
    }
}
