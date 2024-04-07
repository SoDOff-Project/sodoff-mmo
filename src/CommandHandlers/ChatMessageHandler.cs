using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;
using sodoffmmo.Management;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("SCM")]
class ChatMessageHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        string message = receivedObject.Get<NetworkObject>("p").Get<string>("chm");
        if (ManagementCommandProcessor.ProcessCommand(message, client))
            return Task.CompletedTask;
        if (!Configuration.ServerConfiguration.EnableChat) {
            ChatDisabled(client);
        } else {
            Chat(client, message);
        }
        return Task.CompletedTask;
    }

    public void ChatDisabled(Client client) {
        NetworkObject cmd = new();
        NetworkObject data = new();
        data.Add("arr", new string[] { "CMR", "-1", "-1", "1", "Unfortunately, chat has been disabled by server administrators", "", "1", "Server" });
        cmd.Add("c", "CMR");
        cmd.Add("p", data);

        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Send(packet);
    }

    public void Chat(Client client, string message) {
        if (client.PlayerData.DiplayName == "")
            return;

        NetworkObject cmd = new();
        NetworkObject data = new();
        data.Add("arr", new string[] { "CMR", "-1", client.PlayerData.Uid, "1", message, "", "1", client.PlayerData.DiplayName });
        cmd.Add("c", "CMR");
        cmd.Add("p", data);

        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Room.Send(packet, client);

        cmd = new();
        data = new();
        data.Add("arr", new string[] { "SCA", "-1", "1", message, "", "1" });
        cmd.Add("c", "SCA");
        cmd.Add("p", data);
        packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Send(packet);
    }
}
