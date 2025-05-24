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
        if (client.TempMuted) {
            ClientMuted(client);
            return Task.CompletedTask;
        }
        if (!Configuration.ServerConfiguration.EnableChat && !client.Room.AllowChatOverride) {
            ChatDisabled(client);
        } else {
            Chat(client, message);
        }
        return Task.CompletedTask;
    }

    public void ChatDisabled(Client client) {
        client.Send(Utils.BuildServerSideMessage("Unfortunately, chat has been disabled by server administrators", "Server"));
    }

    public void ClientMuted(Client client) {
        client.Send(Utils.BuildServerSideMessage("You have been muted by the moderators", "Server"));
    }

    public void Chat(Client client, string message) {
        if (Configuration.ServerConfiguration.Authentication >= AuthenticationMode.RequiredForChat && client.PlayerData.DiplayName == "placeholder") {
            client.Send(Utils.BuildServerSideMessage("You must be authenticated to use the chat", "Server"));
            return;
        }

        client.Room.Send(Utils.BuildChatMessage(client.PlayerData.Uid, message, client.PlayerData.DiplayName), client);
        DiscordManager.SendMessage(client.PlayerData.DiplayName+">"+message, client.Room);

        NetworkObject cmd = new();
        NetworkObject data = new();
        data.Add("arr", new string[] { "SCA", "-1", "1", message, "", "1" });
        cmd.Add("c", "SCA");
        cmd.Add("p", data);
        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Send(packet);
    }
}
