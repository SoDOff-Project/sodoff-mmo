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
        if (!Configuration.ServerConfiguration.EnableChat && !client.Room.AllowChatOverride) {
            ChatDisabled(client, message);
        } else if (Configuration.ServerConfiguration.Authentication >= AuthenticationMode.RequiredForChat && client.PlayerData.VikingId != 0) {
            ChatRequiredAuthentication(client, message);
        } else if (client.Muted) {
            ClientMuted(client, message);
        } else {
            Chat(client, message);
        }
        return Task.CompletedTask;
    }

    // NOTE: message is passed also for functions that refuse to send messages (this can be useful at the level of the chat moderation plugin)

    public void ChatDisabled(Client client, string message) {
        client.Send(Utils.BuildServerSideMessage("Unfortunately, chat has been disabled by server administrators", "Server"));
    }

    public void ChatRequiredAuthentication(Client client, string message) {
        client.Send(Utils.BuildServerSideMessage("You must be authenticated to use the chat", "Server"));
    }

    public void ClientMuted(Client client, string message) {
        client.Send(Utils.BuildServerSideMessage("You have been muted by the moderators", "Server"));
    }

    public void Chat(Client client, string message) {
        client.Room.Send(Utils.BuildChatMessage(client.PlayerData.Uid, message, client.PlayerData.DisplayName), client);
        // NOTE: using DisplayName not VikingName here for consistency with client behavior, preventing "placeholder" in not authenticated modes and support for changing name while logged on MMO

        NetworkObject cmd = new();
        NetworkObject data = new();
        data.Add("arr", new string[] { "SCA", "-1", "1", message, "", "1" });
        cmd.Add("c", "SCA");
        cmd.Add("p", data);
        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Send(packet);
    }
}
