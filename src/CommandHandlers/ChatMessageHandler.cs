using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;
using sodoffmmo.Management;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("SCM")]
class ChatMessageHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        string message = receivedObject.Get<NetworkObject>("p").Get<string>("chm");
        string messageFilter = message.ToLower();
        if (ManagementCommandProcessor.ProcessCommand(message, client))
            return Task.CompletedTask;
        if (client.TempMuted)
        {
            ClientMuted(client);
            return Task.CompletedTask;
        }

        messageFilter = messageFilter.Replace(",", "");//Hardcoded to remove commas from the message due to some issues
        
        foreach (var replaceable in ChatFilter.ChatFilterConfiguration.charsToRemove)//Rolls through the entire character replacement list
        {
            if (replaceable.Contains(","))//Checks if the replace string is valid (has a comma)
            {
                string char1 = replaceable.Remove(replaceable.LastIndexOf(','));//Gets the character to be replaced
                string char2 = replaceable.Remove(0, replaceable.LastIndexOf(',') + 1);//Gets the replacement
                messageFilter = messageFilter.Replace(char1, char2);
            }
        }
        messageFilter = messageFilter.Replace(" ", "");

        foreach (var banned in ChatFilter.ChatFilterConfiguration.FilteredWords)//Rolls through the entire denylist
        {
            if (messageFilter.Contains(banned))//Checks if your message contains a word from the denylist
            {
                Console.WriteLine("Banned message from " + client.PlayerData.Uid + " (" + client.PlayerData.DiplayName + "): \"" + messageFilter + "\", \"" + message + "\" in game: " + client.PlayerData.ZoneName);
                ClientFilter(client);
                if (!ChatFilter.ChatFilterConfiguration.blockedMessage.Equals(""))
                    Chat(client, ChatFilter.ChatFilterConfiguration.blockedMessage);//This replaces the user's message with the message in the string. TODO: Add the string to the chatfilter json for the purpose of customization (How do I do that)
                return Task.CompletedTask;
            }
        }

        if (!Configuration.ServerConfiguration.EnableChat && !client.Room.AllowChatOverride)
        {
            ChatDisabled(client);
        }
        else
        {
            Console.WriteLine("Message from " + client.PlayerData.Uid + " (" + client.PlayerData.DiplayName + "): \"" + messageFilter + "\", \"" + message + "\" in game: " + client.PlayerData.ZoneName);
            Chat(client, message);
        }
        return Task.CompletedTask;
    }

    public void ClientFilter(Client client)
    {
        client.Send(Utils.BuildServerSideMessage("This message contains a word or phrase not allowed on this server.", "Server"));
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

        NetworkObject cmd = new();
        NetworkObject data = new();
        data.Add("arr", new string[] { "SCA", "-1", "1", message, "", "1" });
        cmd.Add("c", "SCA");
        cmd.Add("p", data);
        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Send(packet);
    }
}
