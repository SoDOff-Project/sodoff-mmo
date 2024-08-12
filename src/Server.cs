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
        moduleManager.RegisterModules();
        ManagementCommandProcessor.Initialize();
        using Socket listener = new(ipAddress.AddressFamily,
                                    SocketType.Stream,
                                    ProtocolType.Tcp);
        if (IPv6AndIPv4)
            listener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
        listener.Bind(new IPEndPoint(ipAddress, port));

        foreach (var room in Configuration.ServerConfiguration.RoomAlerts) {
            foreach (var alert in room.Value) {
                Console.WriteLine($"Setup alert \"{alert[0]}\" for {room.Key}");
                Room.GetOrAdd(room.Key).AddAlert(new Room.AlertInfo(
                    alert[0], // type
                    float.Parse(alert[1], System.Globalization.CultureInfo.InvariantCulture.NumberFormat), // duration
                    Int32.Parse(alert[2]), Int32.Parse(alert[3]), // start min - max for random start time
                    Int32.Parse(alert[4]), Int32.Parse(alert[5]) // extra parameters for specific alarm types
                ));
            }
        }

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
}
