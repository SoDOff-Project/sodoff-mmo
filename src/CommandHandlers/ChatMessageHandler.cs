using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("SCM")]
class ChatMessageHandler : ICommandHandler {
    public void Handle(Client client, NetworkObject receivedObject) {
        if (!Configuration.ServerConfiguration.EnableChat) {
            ChatDisabled(client, receivedObject);
            return;
        }
        Chat(client, receivedObject);
    }

    public void ChatDisabled(Client client, NetworkObject receivedObject) {
        NetworkObject cmd = new();
        NetworkObject data = new();
        data.Add("arr", new string[] { "CMR", "-1", "-1", "1", "Unfortunately, chat has been disabled by server administrators", "", "1", "Server" });
        cmd.Add("c", "CMR");
        cmd.Add("p", data);

        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Send(packet);
    }

    public void Chat(Client client, NetworkObject receivedObject) {
        string message = receivedObject.Get<NetworkObject>("p").Get<string>("chm");

        NetworkObject cmd = new();
        NetworkObject data = new();
        data.Add("arr", new string[] { "CMR", "-1", client.PlayerData.Uid, "1", message, "", "1", "placeholder" });
        cmd.Add("c", "CMR");
        cmd.Add("p", data);

        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        foreach (var roomClient in client.Room.Clients) {
            if (roomClient != client) {
                roomClient.Send(packet);
            }
        }

        cmd = new();
        data = new();
        data.Add("arr", new string[] { "SCA", "-1", "1", message, "", "1" });
        cmd.Add("c", "SCA");
        cmd.Add("p", data);
        packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Send(packet);
    }
}
