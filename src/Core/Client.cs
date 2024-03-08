using sodoffmmo.Data;
using System;
using System.Net.Sockets;

namespace sodoffmmo.Core;
public class Client {
    static int id;
    static object lck = new();

    public int ClientID { get; private set; }
    public PlayerData PlayerData { get; set; } = new();
    public Room Room { get; set; }

    private readonly Socket socket;
    SocketBuffer socketBuffer = new();
    private volatile bool scheduledDisconnect = false;

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
            LeaveRoom();
            SheduleDisconnect();
        }
    }

    public void LeaveRoom() {
        if (Room != null) {
            Console.WriteLine($"Leave room {Room.Name} IID: {ClientID}");
            Room.RemoveClient(this);
            NetworkObject data = new();
            data.Add("r", Room.Id);
            data.Add("u", ClientID);

            NetworkPacket packet = NetworkObject.WrapObject(0, 1004, data).Serialize();
            foreach (var roomClient in Room.Clients) {
                roomClient.Send(packet);
            }
            Room = null;
        }
    }
    
    public void JoinRoom(Room room) {
        LeaveRoom();
        InvalidatePlayerData();
        Room = room;
        Room.AddClient(this);
        Send(Room.SubscribeRoom());
        UpdatePlayerUserVariables();
    }

    private void UpdatePlayerUserVariables() {
        foreach (Client c in Room.Clients) {
            NetworkObject cmd = new();
            NetworkObject obj = new();
            cmd.Add("c", "SUV");
            obj.Add("MID", c.ClientID);
            cmd.Add("p", obj);
            Send(NetworkObject.WrapObject(1, 13, cmd).Serialize());
        }
    }

    public void InvalidatePlayerData() {
        PlayerData.IsValid = false;
    }

    public void Disconnect() {
        try {
            socket.Shutdown(SocketShutdown.Both);
        } finally {
            socket.Close();
        }
    }

    public void SheduleDisconnect() {
        scheduledDisconnect = true;
    }

    public bool Connected {
        get {
            return socket.Connected && !scheduledDisconnect;
        }
    }
}
