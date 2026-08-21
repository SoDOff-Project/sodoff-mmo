using sodoffmmo.Core;
using sodoffmmo.Data;
using sodoffmmo.Management;
using System;
using System.Net;
using System.Net.Sockets;

namespace sodoffmmo;
public class Server {

    readonly int port;
    readonly IPAddress ipAddress;
    readonly bool IPv6AndIPv4;
    ModuleManager moduleManager = new();

    public Server(IPAddress ipAdress, int port, bool IPv6AndIPv4) {
        this.ipAddress = ipAdress;
        this.port = port;
        this.IPv6AndIPv4 = IPv6AndIPv4;
    }

    public async Task Run() {
        _ = Task.Run(() => ListenTcpAdmin());
        moduleManager.RegisterModules();
        ManagementCommandProcessor.Initialize();
        using Socket listener = new(ipAddress.AddressFamily,
                                    SocketType.Stream,
                                    ProtocolType.Tcp);
        if (IPv6AndIPv4)
            listener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
        listener.Bind(new IPEndPoint(ipAddress, port));

        SpecialRoom.CreateRooms();

        await Listen(listener);
    }

    private async Task Listen(Socket listener) {
        Console.WriteLine($"MMO Server listening on port {port}");
        listener.Listen(100);
        while (true) {
            Socket handler = await listener.AcceptAsync();
            handler.SendTimeout = 200;
            Console.WriteLine($"New connection from {((IPEndPoint)handler.RemoteEndPoint!).Address}");
            _ = Task.Run(() => HandleClient(handler));
        }
    }

    private async Task HandleClient(Socket handler) {
        Client client = new(handler);
        try {
            while (client.Connected) {
                await client.Receive();
                List<NetworkObject> networkObjects = new();
                while (client.TryGetNextPacket(out NetworkPacket packet))
                    networkObjects.Add(packet.GetObject());

                await HandleObjects(networkObjects, client);
            }
        } finally {
            try {
                client.SetRoom(null);
            } catch (Exception) { }
            client.NotifyOnlineStatus(false);
            client.Disconnect();
            Console.WriteLine("Socket disconnected IID: " + client.ClientID);
        }
    }

    private async Task HandleObjects(List<NetworkObject> networkObjects, Client client) {
        foreach (var obj in networkObjects) {
            try {
                short commandId = obj.Get<short>("a");
                CommandHandler handler;
                if (commandId != 13) {
                    if (commandId == 0 || commandId == 1)
                        Console.WriteLine($"System command: {commandId} IID: {client.ClientID}");
                    handler = moduleManager.GetCommandHandler(commandId);
                } else
                    handler = moduleManager.GetCommandHandler(obj.Get<NetworkObject>("p").Get<string>("c"));
                Task task = handler.Handle(client, obj.Get<NetworkObject>("p"));
                if (!handler.RunInBackground)
                    await task;
            } catch (Exception ex) {
                Console.WriteLine($"Exception IID: {client.ClientID} - {ex}");
            }
        }
    }

    private async Task ListenTcpAdmin() {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 9934);
        listener.Start();
        Console.WriteLine("MMO Admin TCP Server listening on port 9934");
        while (true) {
            try {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                string? line = await reader.ReadLineAsync();
                if (line != null && line.StartsWith("SBE|")) {
                    string[] parts = line.Split('|');
                    if (parts.Length == 4) {
                        string uid = parts[1];
                        string fromUid = parts[2];
                        string cmdType = parts[3];
                        Console.WriteLine($"Sending SBE to user {uid} from {fromUid} cmd {cmdType}");
                        foreach (var room in Room.AllRooms()) {
                            foreach (var mmoClient in room.Clients) {
                                if (mmoClient.PlayerData?.Uid == uid) {
                                    sodoffmmo.Data.NetworkObject cmd = new sodoffmmo.Data.NetworkObject();
                                    cmd.Add("c", "SBE");
                                    sodoffmmo.Data.NetworkObject payload = new sodoffmmo.Data.NetworkObject();
                                    payload.Add("arr", new string[] { "SBE", "", fromUid, uid, cmdType });
                                    cmd.Add("p", payload);
                                    mmoClient.Send(sodoffmmo.Data.NetworkObject.WrapObject(1, 13, cmd).Serialize());
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("TCP Admin Error: " + ex.Message);
            }
        }
    }
}
