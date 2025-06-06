using sodoffmmo.Data;
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace sodoffmmo.Core;
public class Client {
    static int id;
    static object lck = new();

                                  // VikingId, Reason (nullable)
    public static readonly Dictionary<int, string?> MutedList;
    public static readonly Dictionary<int, string?> BannedList;
    
    static Client() { // Runs when Client class is loaded
        if (File.Exists("muted_banned_users.json")) {
            try {
                MutedBannedData? data = JsonSerializer.Deserialize<MutedBannedData>(File.ReadAllText("muted_banned_users.json"));
                if (data != null) {
                    MutedList = data.MutedList ?? new();
                    BannedList = data.BannedList ?? new();
                    return;
                }
            } catch {
                Console.WriteLine("Data for Muted/Banned users wasn't read. Is it corrupted?");
            }
        }
        MutedList = new();
        BannedList = new();
    }

    public int ClientID { get; private set; }
    public PlayerData PlayerData { get; set; } = new();
    public Room? Room { get; private set; }
    // Used by ban system for smooth unban transitions.
    public Room? ReturnRoomOnPardon { get; private set; } = null;
    public bool OldApi { get; set; } = false;

    public bool TempMuted { get; set; } = false;
    public bool RealMuted { get; set; } = false;
    public bool Banned { get; set; } = false;
    public bool Muted() => TempMuted || RealMuted || Banned;

    private readonly Socket socket;
    SocketBuffer socketBuffer = new();
    private volatile bool scheduledDisconnect = false;
    private readonly object clientLock = new();

    public Client(Socket clientSocket) {
        socket = clientSocket;
        lock (lck) {
            ClientID = ++id;
        }
    }

    public async Task Receive() {
        byte[] buffer = new byte[2048];
        int len = await socket.ReceiveAsync(buffer, SocketFlags.None);
        if (len == 0)
            throw new SocketException();
        socketBuffer.Write(buffer, len);
    }

    public bool TryGetNextPacket(out NetworkPacket packet) {
        return socketBuffer.ReadPacket(out packet);
    }

    public void Send(NetworkPacket packet) {
        try {
            socket.Send(packet.SendData);
        } catch (SocketException) {
            ScheduleDisconnect();
        }
    }

    public void SetRoom(Room? room) {
        lock(clientLock) {
            // set variable player data as not valid, but do not reset all player data
            PlayerData.IsValid = false;

            ReturnRoomOnPardon = room; // This is needed so that users are put where they're supposed to when being unbanned.
            if (Room != null && (!Banned || room?.Name == "LIMBO" || !Room.Name.StartsWith("BannedUserRoom_"))) { // If user is in the ban room they're just fine exactly where they are.
                Console.WriteLine($"Leave room: {Room.Name} (id={Room.Id}, size={Room.ClientsCount}) IID: {ClientID}");
                Room.RemoveClient(this);

                NetworkObject data = new();
                data.Add("r", Room.Id);
                data.Add("u", ClientID);
                Room.Send(NetworkObject.WrapObject(0, 1004, data).Serialize());
            }

            if (Banned && room?.Name != "LIMBO") {
                if (room != null) {
                    Room = Room.GetOrAdd("BannedUserRoom_" + ClientID, autoRemove: true);
                    if (Room.Clients.Contains(this)) {
                        Send(Room.RespondJoinRoom());
                    } else {
                        Room.AddClient(this);
                    }
                    Send(Room.SubscribeRoom());
                } else {
                    Room?.RemoveClient(this);
                    Room = null;
                }
            } else {
                // set new room (null when SetRoom is used as LeaveRoom)
                Room = room;

                if (Room != null) {
                    Console.WriteLine($"Join room: {Room.Name} RoomID (id={Room.Id}, size={Room.ClientsCount}) IID: {ClientID}");
                    Room.AddClient(this);

                    Send(Room.SubscribeRoom());
                    if (Room.Name != "LIMBO") UpdatePlayerUserVariables(); // do not update user vars if room is limbo
                }
            }

        }
    }

    private void UpdatePlayerUserVariables() {
        foreach (Client c in Room.Clients) {
            NetworkObject cmd = new();
            NetworkObject obj = new();
            cmd.Add("c", "SUV");
            if (OldApi) {
                obj.Add("MID", c.ClientID.ToString());
            } else {
                obj.Add("MID", c.ClientID);
            }
            cmd.Add("p", obj);
            Send(NetworkObject.WrapObject(1, 13, cmd).Serialize());
        }
    }

    public void Disconnect() {
        try {
            socket.Shutdown(SocketShutdown.Both);
        } finally {
            socket.Close();
        }
    }

    public void ScheduleDisconnect() {
        if (Room != null) {
            // quiet remove from room (to avoid issues in Room.Send)
            //  - do not change Room value here
            //  - full remove will be will take place Server.HandleClient (before real disconnected)
            Room.RemoveClient(this);
        }
        scheduledDisconnect = true;
    }

    public bool Connected {
        get {
            return socket.Connected && !scheduledDisconnect;
        }
    }
    public static void SaveMutedBanned() {
        File.WriteAllText("muted_banned_users.json", JsonSerializer.Serialize(new MutedBannedData {
            MutedList = MutedList,
            BannedList = BannedList
        }, new JsonSerializerOptions {
             WriteIndented = true
        }));
    }
}

internal sealed class MutedBannedData {
    public Dictionary<int, string?>? MutedList { get; set; }
    public Dictionary<int, string?>? BannedList { get; set; }
}
