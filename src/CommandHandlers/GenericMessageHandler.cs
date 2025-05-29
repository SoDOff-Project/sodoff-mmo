using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;
using sodoffmmo.Management;

namespace sodoffmmo.CommandHandlers;

[CommandHandler(7)]
class GenericMessageHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        NetworkPacket packet = NetworkObject.WrapObject(0, 7, receivedObject).Serialize();
        if (receivedObject.Get<object>("m") is string message) {
            if (message.StartsWith("C:")) { // chat
                if (!ChatMessageHandler.CheckIllegalName(client)) {
                    bool sent = false;
                    if (int.TryParse(message.AsSpan(2), out int num)) {
                        string? text = Configuration.GetVersioned(num, client.GameVersion, Configuration.CannedChat);
                        if (text != null) {
                            DiscordManager.SendPlayerBasedMessage("", ":loudspeaker: {1}>"+text, client.Room, client.PlayerData);
                            sent = true;
                        }
                    }
                    if (!sent) DiscordManager.SendPlayerBasedMessage(message.Substring(2), ":loudspeaker: {1}>*Unknown quickchat {0}*", client.Room, client.PlayerData);
                } else {
                    return Task.CompletedTask;
                }
            } else if (message.StartsWith("EID:")) { // Emoticons
                bool sent = false;
                if (int.TryParse(message.AsSpan(4), out int num)) {
                    string? text = Configuration.GetVersioned(num, client.GameVersion, Configuration.Emoticons);
                    if (text != null) {
                        DiscordManager.SendPlayerBasedMessage("", ":wireless: {1} used "+text, client.Room, client.PlayerData);
                        sent = true;
                    }
                }
                if (!sent) DiscordManager.SendPlayerBasedMessage(message.Substring(4), ":wireless: {1} used *unknown emoticon {0}*", client.Room, client.PlayerData);
            } else if (message.StartsWith("AID:")) { // Actions
                bool sent = false;
                if (int.TryParse(message.AsSpan(4), out int num)) {
                    string? text = Configuration.GetVersioned(num, client.GameVersion, client.PlayerData.EMDDriving() ? Configuration.EMDCarActions : Configuration.Actions);
                    if (text != null) {
                        DiscordManager.SendPlayerBasedMessage("", ":dancer: {1} performed "+text, client.Room, client.PlayerData);
                        sent = true;
                    }
                }
                if (!sent) DiscordManager.SendPlayerBasedMessage(message.Substring(4), ":dancer: {1} performed *unknown action {0}*", client.Room, client.PlayerData);
            }
        }
        client.Room.Send(packet);
        return Task.CompletedTask;
    }
}
