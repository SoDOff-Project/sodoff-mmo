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
        _ = Task.Run(() => ListenHttpAdmin());
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

    private async Task ListenHttpAdmin() {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9934/");
        listener.Start();
        Console.WriteLine("MMO Admin HTTP Server listening on port 9934");
        while (true) {
            HttpListenerContext context = await listener.GetContextAsync();
            try {
                if (context.Request.Url!.AbsolutePath == "/Admin/SendBuddyEvent") {
                    string? uid = context.Request.QueryString["uid"];
                    string? fromUid = context.Request.QueryString["fromUid"];
                    string? cmdType = context.Request.QueryString["cmdType"];

                    if (!string.IsNullOrEmpty(uid)) {
                        Console.WriteLine($"Sending SBE to user {uid} from {fromUid} cmd {cmdType}");
                        foreach (var room in Room.AllRooms()) {
                            foreach (var client in room.Clients) {
                                if (client.PlayerData?.Uid == uid) {
                                    sodoffmmo.Data.NetworkObject cmd = new sodoffmmo.Data.NetworkObject();
                                    cmd.Add("c", "SBE");
                                    sodoffmmo.Data.NetworkObject payload = new sodoffmmo.Data.NetworkObject();
                                    payload.Add("arr", new string[] { "SBE", "", fromUid ?? "", uid, cmdType ?? "0" });
                                    cmd.Add("p", payload);
                                    client.Send(sodoffmmo.Data.NetworkObject.WrapObject(1, 13, cmd).Serialize());
                                }
                            }
                        }
                    }
                    context.Response.StatusCode = 200;
                } else if (context.Request.Url!.AbsolutePath == "/Admin/GetBuddyLocation") {
                    string? uid = context.Request.QueryString["uid"];
                    string response = "";
                    if (!string.IsNullOrEmpty(uid)) {
                        foreach (var room in Room.AllRooms()) {
                            foreach (var client in room.Clients) {
                                if (client.PlayerData?.Uid == uid) {
                                    response = room.Name + "|" + room.Id + "|" + client.ClientID;
                                    break;
                                }
                            }
                            if (response != "") break;
                        }
                    }
                    var buffer = System.Text.Encoding.UTF8.GetBytes(response);
                    context.Response.StatusCode = 200;
                    context.Response.ContentLength64 = buffer.Length;
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                } else {
                    context.Response.StatusCode = 404;
                }
            } catch (Exception ex) {
                Console.WriteLine("HTTP Admin Error: " + ex.Message);
                context.Response.StatusCode = 500;
            } finally {
                context.Response.Close();
            }
        }
    }
}
