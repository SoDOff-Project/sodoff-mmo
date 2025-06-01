using Discord.WebSocket;
using Discord;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using sodoffmmo.Core;
using Discord.Rest;
using sodoffmmo.Data;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace sodoffmmo.Management;

class DiscordManager {
    private static readonly ConcurrentQueue<QueuedMessage> MessageQueue = new();
    private static DiscordSocketClient BotClient;
    private static SocketTextChannel BotChannel;

    public static void Initialize() {
        if (Configuration.DiscordBotConfig?.Disabled == false) { // Bot config isn't null or disabled.
            try {
                string botTokenPath = Path.Combine(Path.GetDirectoryName(typeof(Configuration).Assembly.Location), "discord_bot_token");
                if (File.Exists(botTokenPath)) {
                    string? token = new StreamReader(botTokenPath).ReadLine();
                    if (token != null) {
                        if (Configuration.DiscordBotConfig.Server != 0) {
                            if (Configuration.DiscordBotConfig.Channel != 0) {
                                Task.Run(() => RunBot(token));
                                return;
                            }
                            Log("Discord bot was not started because the ServerID isn't set. Configure it in appsettings.json or disable it altogether.", ConsoleColor.Yellow);
                            return;
                        }
                        Log("Discord bot was not started because the ChannelID isn't set. Configure it in appsettings.json or disable it altogether.", ConsoleColor.Yellow);
                        return;
                    }
                }
                Log("Discord bot was not started because the token wasn't found. Create the file containing the bot token (see instructions src/appsettings.json) or disable it altogether in appsettings.json.", ConsoleColor.Yellow);
            } catch (Exception e) {
                Log(e.ToString(), ConsoleColor.Red);
            }
        }
    }

    private static async Task RunBot(string token) {
        BotClient = new DiscordSocketClient(new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent,
            AlwaysDownloadUsers = true
        });
        BotClient.Ready += async () => {
            BotChannel = BotClient.GetGuild(Configuration.DiscordBotConfig!.Server).GetTextChannel(Configuration.DiscordBotConfig!.Channel);
        };
        BotClient.MessageReceived += OnDiscordMessage;
        await BotClient.LoginAsync(TokenType.Bot, token);
        await BotClient.StartAsync();
        Log("Discord bot is running.");
        while (true) {
            try {
                if (!MessageQueue.IsEmpty && MessageQueue.TryDequeue(out QueuedMessage message)) {
                    SocketTextChannel? destination = BotChannel;
                    if (message.room != null) {
                        if (message.room.Name == "LIMBO" || message.room.Name.StartsWith("BannedUserRoom_")) continue; // Don't log stuff in LIMBO or Banned User.
                        string room = message.roomName!;
                        destination = BotChannel.Threads.SingleOrDefault(x => x.Name == room);
                        if (destination == null) {
                            // A discord channel can have up to 1000 active threads
                            // and an unlimited number of archived threads.
                            if (message.room.AutoRemove) {
                                // Set temporary rooms (such as user rooms) to shorter archive times.
                                destination = await BotChannel.CreateThreadAsync(room, autoArchiveDuration: ThreadArchiveDuration.OneHour);
                            } else {
                                // Persistent rooms should stay around for longer.
                                destination = await BotChannel.CreateThreadAsync(room, autoArchiveDuration: ThreadArchiveDuration.OneWeek);
                            }
                        }
                    }
                    await destination.SendMessageAsync(message.msg);
                }
            } catch (Exception e) { // In case something goes wrong for any reason, don't "kill" the bot.
                Log(e.ToString(), ConsoleColor.Red);
            }
        }
    }

    // For handling commands sent directly in the channel.
    private static async Task OnDiscordMessage(SocketMessage arg) {
        // Check that message is from a user in the correct place.
        bool roomChannel = arg.Channel is SocketThreadChannel thr && thr.ParentChannel.Id == BotChannel.Id;
        if (!(
                roomChannel ||
                arg.Channel.Id == BotChannel.Id
            ) ||
            arg is not SocketUserMessage socketMessage ||
            socketMessage.Author.IsBot
        ) return;
        // Get the room if it exists.
        Room[]? room = null;
        string? roomName = null;
        if (roomChannel) {
            roomName = arg.Channel.Name;
            if (Room.Exists(arg.Channel.Name)) {
                room = new Room[1]{Room.Get(arg.Channel.Name)};
            } else if (Configuration.DiscordBotConfig!.Merges.TryGetValue(arg.Channel.Name, out string[]? zones) && zones?.Length > 0) {
                room = new Room[zones.Length];
                for (int i=0;i<zones.Length;i++) {
                    if (Room.Exists(zones[i])) {
                        room[i] = Room.Get(zones[i]);
                    }
                }
            }
        }

        // Perms
        Role role = Role.User;
        if (arg.Author is IGuildUser user) {
            if (user.RoleIds.Contains(Configuration.DiscordBotConfig!.RoleAdmin)) {
                role = Role.Admin;
            } else if (user.RoleIds.Contains(Configuration.DiscordBotConfig!.RoleModerator)) {
                role = Role.Admin;
            }
        }

        string[] message = socketMessage.Content.Split(' ');
        if (message.Length >= 1) {
            string[] everywhereCommands = new string[] {
                // Denotes commands that work the same or similarly
                // regardless of whether you're in a room thread.
                "!freechat <bool: enabled> - Enable/Disable free chat in all rooms.",
                "!tempmute <int: vikingid> [string... reason] - Mute someone (until they reconnect).",
                "!untempmute <int: vikingid> - Revoke a tempmute.",
                "!mute <int: vikingid> [string... reason] - Mute a vikingid.",
                "!unmute <int: vikingid> - Revoke a mute.",
                "!ban <int: vikingid> [string... reason] - Ban a vikingid from MMO.",
                "!unban <int: vikingid> - Revoke a ban.",
                "!pardon <int: vikingid> - Alias for !unban",
                "!mutelist - Get a list of muted users.",
                "!banlist - Get a list of banned users.",
            };
            if (message[0] == "!freechat") {
                if (role < Role.Moderator) {
                    await arg.Channel.SendMessageAsync("You don't have permission to use this!", messageReference: arg.Reference);
                    return;
                }
                // freechat command
                if (message.Length > 1) {
                    bool enable;
                    int val = message[1][0] & 0b11011111;
                    if (val == 'Y' || val == 'T') {
                        enable = true;
                    } else if (val == 'N' || val == 'F') {
                        enable = false;
                    } else {
                        await arg.Channel.SendMessageAsync("Input a boolean or yes/no value!", messageReference: arg.Reference);
                        return;
                    }
                    Configuration.ServerConfiguration.EnableChat = enable;
                    await arg.Channel.SendMessageAsync($"Chat {(enable?"en":"dis")}abled!", messageReference: arg.Reference);
                } else {
                    await arg.Channel.SendMessageAsync("Input a value!", messageReference: arg.Reference);
                }
            } else if (message[0] == "!tempmute") {
                if (role < Role.Moderator) {
                    await arg.Channel.SendMessageAsync("You don't have permission to use this!", messageReference: arg.Reference);
                    return;
                }
                // tempmute command
                if (message.Length > 1 && int.TryParse(message[1], out int id)) {
                    IEnumerable<Client> vikings = FindViking(id);
                    if (vikings.Any()) {
                        bool sent = false;
                        string? reason = null;
                        if (message.Length > 2) {
                            reason = string.Join(' ', message, 2, message.Length - 2);
                        }
                        foreach (Client player in vikings) {
                            if (player.Muted) {
                                if (Client.MutedList.TryGetValue(id, out string? rreason)) {
                                    string msg = "This user is already muted.";
                                    if (rreason != null && rreason.Length > 0) msg += " Reason: "+rreason;
                                    if (!sent) await arg.Channel.SendMessageAsync(msg, messageReference: arg.Reference);
                                } else {
                                    if (!sent) await arg.Channel.SendMessageAsync("This user is already tempmuted.", messageReference: arg.Reference);
                                }
                            } else {
                                player.Muted = true;
                                string msg = $"Tempmuted {SanitizeString(player.PlayerData.DiplayName)}";
                                if (reason != null) {
                                    msg += ". Reason: "+reason;
                                }
                                if (!sent) SendMessage(msg, roomName: roomName);
                            }
                            sent = true;
                        }
                    } else {
                        await arg.Channel.SendMessageAsync("User with that ID is not online.", messageReference: arg.Reference);
                    }
                } else {
                    await arg.Channel.SendMessageAsync("Input a valid vikingid!", messageReference: arg.Reference);
                }
            } else if (message[0] == "!untempmute") {
                if (role < Role.Moderator) {
                    await arg.Channel.SendMessageAsync("You don't have permission to use this!", messageReference: arg.Reference);
                    return;
                }
                // un-tempmute command
                if (message.Length > 1 && int.TryParse(message[1], out int id)) {
                    IEnumerable<Client> vikings = FindViking(id);
                    if (vikings.Any()) {
                        bool sent = false;
                        foreach (Client player in vikings) {
                            if (player.Muted) {
                                if (Client.MutedList.ContainsKey(id)) {
                                    if (!sent) await arg.Channel.SendMessageAsync("This user is true muted. To unmute them, use !unmute", messageReference: arg.Reference);
                                } else {
                                    player.Muted = false;
                                    if (!sent) SendMessage($"Un-tempmuted {SanitizeString(player.PlayerData.DiplayName)}.", roomName: roomName);
                                }
                            } else {
                                if (!sent) await arg.Channel.SendMessageAsync("This user is not muted.", messageReference: arg.Reference);
                            }
                            sent = true;
                        }
                    } else {
                        await arg.Channel.SendMessageAsync("User with that ID is not online.", messageReference: arg.Reference);
                    }
                } else {
                    await arg.Channel.SendMessageAsync("Input a valid vikingid!", messageReference: arg.Reference);
                }
            } else if (message[0] == "!mute") {
                if (role < Role.Moderator) {
                    await arg.Channel.SendMessageAsync("You don't have permission to use this!", messageReference: arg.Reference);
                    return;
                }
                // true mute command
                if (message.Length > 1 && int.TryParse(message[1], out int id)) {
                    string? name = null;
                    IEnumerable<Client> vikings = FindViking(id);
                    foreach (Client player in vikings) {
                        name = SanitizeString(player.PlayerData.DiplayName);
                        player.Muted = true;
                    }
                    if (Client.MutedList.TryGetValue(id, out string? rreason)) {
                        string msg = "This user is already muted.";
                        if (rreason != null && rreason.Length > 0) msg += " Reason: "+rreason;
                        await arg.Channel.SendMessageAsync(msg, messageReference: arg.Reference);
                    } else {
                        string? reason = null;
                        if (message.Length > 2) {
                            reason = string.Join(' ', message, 2, message.Length - 2);
                        }
                        Client.MutedList.Add(id, reason);
                        Client.SaveMutedBanned();
                        string msg = $"Muted *VikingID {id}*";
                        if (name != null) msg = $"Muted {name}";
                        if (reason != null) {
                            msg += ". Reason: " + reason;
                        }
                        SendMessage(msg, roomName: roomName);
                    }
                } else {
                    await arg.Channel.SendMessageAsync("Input a valid vikingid!", messageReference: arg.Reference);
                }
            } else if (message[0] == "!unmute") {
                if (role < Role.Moderator) {
                    await arg.Channel.SendMessageAsync("You don't have permission to use this!", messageReference: arg.Reference);
                    return;
                }
                // unmute command
                if (message.Length > 1 && int.TryParse(message[1], out int id)) {
                    string? name = null;
                    IEnumerable<Client> vikings = FindViking(id);
                    if (vikings.Any()) {
                        foreach (Client player in vikings) {
                            if (player.Muted) {
                                name = SanitizeString(player.PlayerData.DiplayName);
                                player.Muted = false;
                            }
                        }
                    }
                    bool listRemoved = Client.MutedList.Remove(id);
                    if (listRemoved || name != null) { // name is only set if there is an unmute
                        if (listRemoved) Client.SaveMutedBanned();
                        if (name != null) {
                            SendMessage($"Unmuted {name}.", roomName: roomName);
                        } else {
                            await arg.Channel.SendMessageAsync($"Unmuted *VikingID {id}*.", messageReference: arg.Reference);
                        }
                    } else {
                        await arg.Channel.SendMessageAsync("This user is not muted.", messageReference: arg.Reference);
                    }
                } else {
                    await arg.Channel.SendMessageAsync("Input a valid vikingid!", messageReference: arg.Reference);
                }
            } else if (message[0] == "!ban") {
                if (role < Role.Moderator) {
                    await arg.Channel.SendMessageAsync("You don't have permission to use this!", messageReference: arg.Reference);
                    return;
                }
                // ban command
                if (message.Length > 1 && int.TryParse(message[1], out int id)) {
                    string? name = null;
                    foreach (Client player in FindViking(id)) {
                        name = SanitizeString(player.PlayerData.DiplayName);
                        player.Banned = true;
                        player.SetRoom(player.Room); // This will run the client through the 'if Banned' conditions.
                    }
                    if (Client.BannedList.TryGetValue(id, out string? rreason)) {
                        string msg = "This user is already banned.";
                        if (rreason != null && rreason.Length > 0) msg += " Reason: "+rreason;
                        await arg.Channel.SendMessageAsync(msg, messageReference: arg.Reference);
                    } else {
                        string? reason = null;
                        if (message.Length > 2) {
                            reason = string.Join(' ', message, 2, message.Length - 2);
                        }
                        Client.BannedList.Add(id, reason);
                        Client.SaveMutedBanned();
                        string msg = $"Banned *VikingID {id}*";
                        if (name != null) msg = $"Banned {name}";
                        if (reason != null) {
                            msg += ". Reason: " + reason;
                        }
                        SendMessage(msg, roomName: roomName);
                    }
                } else {
                    await arg.Channel.SendMessageAsync("Input a valid vikingid!", messageReference: arg.Reference);
                }
            } else if (message[0] == "!unban" || message[0] == "!pardon") {
                if (role < Role.Moderator) {
                    await arg.Channel.SendMessageAsync("You don't have permission to use this!", messageReference: arg.Reference);
                    return;
                }
                // unban command
                if (message.Length > 1 && int.TryParse(message[1], out int id)) {
                    if (Client.BannedList.Remove(id)) {
                        Client.SaveMutedBanned();
                        string? playername = null;
                        foreach (Room rooom in Room.AllRooms()) {
                            foreach (Client player in rooom.Clients) {
                                if (player.PlayerData.VikingId == id) {
                                    playername = SanitizeString(player.PlayerData.DiplayName);
                                    player.Banned = false;
                                    player.SetRoom(player.ReturnRoomOnPardon); // Put the player back.
                                }
                            }
                        }
                        if (playername != null) {
                            SendMessage($"Unbanned {playername}.", roomName: roomName);
                        } else {
                            await arg.Channel.SendMessageAsync($"Unbanned *VikingID {id}*.", messageReference: arg.Reference);
                        }
                    } else {
                        await arg.Channel.SendMessageAsync("This user is not banned.", messageReference: arg.Reference);
                    }
                } else {
                    await arg.Channel.SendMessageAsync("Input a valid vikingid!", messageReference: arg.Reference);
                }
            } else if (message[0] == "!mutelist") {
                // mutelist command
                if (Client.MutedList.Count > 0) {
                    Dictionary<int, string> clients = new();
                    foreach (Room rooom in Room.AllRooms()) {
                        foreach (Client player in rooom.Clients) {
                            int id = player.PlayerData.VikingId;
                            if (Client.MutedList.ContainsKey(id)) {
                                clients.TryAdd(id, player.PlayerData.DiplayName);
                            }
                        }
                    }
                    string msg = "Muted Players:";
                    foreach (KeyValuePair<int, string?> play in Client.MutedList) {
                        if (clients.TryGetValue(play.Key, out string? name)) {
                            msg += $"\n* {SanitizeString(name ?? "")} ||VikingId: {play.Key}||";
                        } else {
                            msg += $"\n* *Offline Viking {play.Key}*";
                        }
                        if (play.Value != null) {
                            msg += $"; Reason: {play.Value}";
                        }
                    }
                    SendMessage(msg, roomName: roomName);
                } else {
                    await arg.Channel.SendMessageAsync("No-one is muted.", messageReference: arg.Reference);
                }
            } else if (message[0] == "!banlist") {
                // mutelist command
                if (Client.BannedList.Count > 0) {
                    Dictionary<int, string> clients = new();
                    foreach (Room rooom in Room.AllRooms()) {
                        foreach (Client player in rooom.Clients) {
                            int id = player.PlayerData.VikingId;
                            if (Client.BannedList.ContainsKey(id)) {
                                clients.TryAdd(id, player.PlayerData.DiplayName);
                            }
                        }
                    }
                    string msg = "Banned Players:";
                    foreach (KeyValuePair<int, string?> play in Client.BannedList) {
                        if (clients.TryGetValue(play.Key, out string? name)) {
                            msg += $"\n* {SanitizeString(name ?? "")} ||VikingId: {play.Key}||";
                        } else {
                            msg += $"\n* *Offline Viking {play.Key}*";
                        }
                        if (play.Value != null) {
                            msg += $"; Reason: {play.Value}";
                        }
                    }
                    SendMessage(msg, roomName: roomName);
                } else {
                    await arg.Channel.SendMessageAsync("No-one is banned.", messageReference: arg.Reference);
                }
            } else if (roomChannel) {
                if (room != null) {
                    if (message[0] == "!help") {
                        // help command (room ver.)
                        await arg.Channel.SendMessageAsync(string.Join(Environment.NewLine+"* ", new string[] {
                                "### List of commands:",
                                "!help - Prints the help message. That's this message right here!",
                                "!list - Lists all players in this room.",
                                "!say <string... message> - Say something in this room."
                            }.Concat(everywhereCommands)
                        ));
                    } else if (message[0] == "!list") {
                        // list command (room ver.)
                        if (room.Sum(r => r.ClientsCount) > 0) {
                            string msg = "Players in Room:";
                            foreach (Room ro in room) {
                                foreach (Client player in ro.Clients) {
                                    string vikingid;
                                    if (player.PlayerData.VikingId != 0) {
                                        vikingid = "||VikingId: "+player.PlayerData.VikingId+"||";
                                    } else {
                                        vikingid = "||(Not Authenticated!)||";
                                    }
                                    msg += $"\n* {SanitizeString(player.PlayerData.DiplayName)} {vikingid}";
                                    if (player.Muted) {
                                        msg += " (:mute: Muted)";
                                    }
                                }
                            }
                            SendMessage(msg, roomName: roomName); // Sent like this in case it's longer than 2000 characters (handled by this method).
                        } else {
                            await arg.Channel.SendMessageAsync("This room is empty.", messageReference: arg.Reference);
                        }
                    } else if (message[0] == "!say") {
                        // say command
                        if (message.Length > 1) {
                            // Due to client-based restrictions, this probably only works in SoD and maybe MaM.
                            // Unless we want to create an MMOAvatar instance for the server "user".
                            // This is because the code for chatlogging is tied to ChatBubble and only ChatBubble.
                            bool sent = false;
                            foreach (Room ro in room) {
                                if (ro.ClientsCount > 0) {
                                    ro.Send(Utils.BuildServerSideMessage(string.Join(' ', message, 1, message.Length-1), "Server"));
                                    sent = true;
                                }
                            }
                            if (sent) {
                                await arg.AddReactionAsync(Emote.Parse(":white_check_mark:"));
                            } else {
                                await arg.Channel.SendMessageAsync("There is no-one in this room to see your message.", messageReference: arg.Reference);
                            }
                        } else {
                            await arg.Channel.SendMessageAsync("You didn't include any message content!", messageReference: arg.Reference);
                        }
                    }
                } else {
                    await arg.Channel.SendMessageAsync("This room is currently unloaded.", messageReference: arg.Reference);
                }
            } else {
                if (message[0] == "!help") {
                    // help command (global)
                    await arg.Channel.SendMessageAsync(string.Join(Environment.NewLine, new string[] {
                        "### List of commands:",
                        "!help - Prints the help message. That's this message right here!",
                        "!rooms - Ping you in all active Room threads you aren't already in.",
                        "        This will add you to all of the room threads. :warning: **This may generate a lot of pings for you!** :warning:",
                        "!list - Lists all players on the server."
                    }.Concat(everywhereCommands)
                    .Append("*Using !help in a room thread yields a different set of commands. Try using !help in a room thread!*")
                    ));
                } else if (message[0] == "!rooms") {
                    // rooms command
                    IEnumerable<RestThreadChannel> threads = await BotChannel.GetActiveThreadsAsync();
                    Console.WriteLine(threads.Count());
                    ulong uid = socketMessage.Author.Id;
                    foreach (RestThreadChannel thread in threads) {
                        // If the user is already in the thread no need to ping them.
                        if (await thread.GetThreadUsersAsync().Flatten().AnyAsync(user => user.Id == uid)) continue;
                    
                        // Ping the user and then delete the message.
                        RestUserMessage ping = await thread.SendMessageAsync("<@"+uid+"> making this thread visible for you ;)");
                        await ping.DeleteAsync();
                    }
                } else if (message[0] == "!list") {
                    // list command (global)
                    string msg = string.Empty;
                    foreach (Room rooom in Room.AllRooms()) {
                        foreach (Client player in rooom.Clients) {
                            if (msg == string.Empty) msg = "Players Online:";
                            
                            string vikingid;
                            if (player.PlayerData.VikingId != 0) {
                                vikingid = "||VikingId: "+player.PlayerData.VikingId+"||";
                            } else {
                                vikingid = "||(Not Authenticated!)||";
                            }
                            msg += $"\n* {SanitizeString(player.PlayerData.DiplayName)} {vikingid} ({rooom.Name})";
                            if (player.Muted) {
                                msg += " (:mute: Muted)";
                            }
                        }
                    }
                    if (msg == string.Empty) {
                        await arg.Channel.SendMessageAsync("Nobody is online.", messageReference: arg.Reference);
                    } else {
                        SendMessage(msg); // Sent like this in case it's longer than 2000 characters (handled by this method).
                    }
                }
            }
        }
    }

    /// <summary>
    /// Send a message through the bot.
    /// If the message is larger than 2000 characters it will be split until if fit.
    /// </summary>
    /// <param name="msg">Message to send</param>
    /// <param name="room">Room to send message in - can be null</param>
    public static void SendMessage(string msg, Room? room=null, string? roomName=null) {
        if (BotClient != null) {
            while (msg.Length > 0) {
                string piece = msg;
                if (msg.Length > 2000) { // Discord character limit is 2000.
                    // Find a good place to break the message, if possible. Otherwise revert to 2000.
                    int breakIndex = msg.LastIndexOf('\n', 2000);
                    if (breakIndex <= 0) breakIndex = msg.LastIndexOf(' ', 2000);
                    if (breakIndex <= 0) breakIndex = 2000;
                    // Split the first part of the message and recycle the rest.
                    piece = msg.Substring(0, breakIndex);
                    msg = msg.Substring(breakIndex);
                } else {
                    // Exit the loop when the time comes.
                    msg = string.Empty;
                }
                MessageQueue.Enqueue(new QueuedMessage {
                    msg = piece,
                    room = room,
                    roomName = roomName ?? room?.Name
                });
            }
        }
    }

    /// <summary>
    /// Sends a message through the bot, but include data about the user and sanitize it.
    /// </summary>
    /// <param name="msg">Message to send</param>
    /// <param name="surround">
    /// Surround the message using formatting.
    /// {0} corresponds to sanitized msg.
    /// {1} correspongs to sanitaize playername.
    /// </param>
    /// <param name="room">Room to send message in - can be null</param>
    /// <param name="player">PlayerData to associate the message with - can be null</param>
    public static void SendPlayerBasedMessage(string msg, string surround="{1} **-->** {0}", Room? room=null, PlayerData? player=null) {
        try {
            if (BotClient != null) {
                // We need to sanitize things first so that people can't
                // use Discord's formatting features inside a chat message.
                msg = SanitizeString(msg);
                string name = SanitizeString(player?.DiplayName ?? "");

                // Define for merged rooms.
                string? roomName = room?.Name;
                if (roomName != null) {
                    foreach (KeyValuePair<string, string[]> pair in Configuration.DiscordBotConfig!.Merges) {
                        if (pair.Value.Contains(roomName)) {
                            roomName = pair.Key;
                            if (roomName != room!.Name) surround = $"({room!.Name}) "+surround;
                            break;
                        }
                    }
                }

                if (player != null) {
                    if (player.VikingId != 0) {
                        surround = $"||VikingId: {player.VikingId}||\n{surround}";
                    } else {
                        surround = $"||(Not Authenticated!)||\n{surround}";
                    }
                }

                // Send the sanitized message, headed with the viking id (if authenticated).
                SendMessage(string.Format(surround, msg, name), room, roomName);
            }
        } catch (Exception e) {
            Log(e.ToString(), ConsoleColor.Red);
        }
    }

    private static string SanitizeString(string str) {
        string sanitizedStr = "";
        for (int i=0;i<str.Length;i++) {
            char c = str[i];
            if (
                c == '\\' ||// Cleanse any attempts to evade the sanitization below
                c == '*' || // Cleanse most text decoration
                c == '_' || // Cleanse some other text decoration
                c == '~' || // Cleanse Strikethrough text decoration
                c == '`' || // Cleanse Code Snippet text decoration
                c == ':' || // Cleanse Links, Emojis, and Timestamps
                (c&0b11111101) == '<' || // Cleanse indirect pings and channel links, and prevents cleansed versions Masked Links with embed disabled from looking strange
                                         // By AND-ing the value by 0b11111101, it will affect both '<' and '>'
                (i > 0 && c == '(' && str[i-1] == ']') // Cleanse Masked Links themselves
            ) {
                sanitizedStr += '\\'; // Add a backslash to escape it.
            } 
            if (c == '@') { // Cleanse pings to @everyone and @here
                sanitizedStr += "`@`";
            } else {
                // Append the character as normal, unless
                // overridden in the single case above.
                sanitizedStr += c;
            }
        }
        return sanitizedStr;
    }

    /// <summary>
    /// Search all rooms for a vikingid, or null if not found.
    /// This method currently returns a list because multiple clients can log in with the same viking at once. This should get fixed properly in the future.
    /// </summary>
    /// <param name="id">Viking ID</param>
    /// <returns>Clients for ID. If there are none then return an empty list.</returns>
    private static IEnumerable<Client> FindViking(int id) {
        List<Client> clients = new();
        foreach (Room room in Room.AllRooms()) {
            foreach (Client player in room.Clients) {
                if (player.PlayerData.VikingId == id) {
                    clients.Add(player);
                }
            }
        }
        return clients;
    }

    private class QueuedMessage {
        public string msg;
        public Room? room;
        public string? roomName;
    }

    private static void Log(string msg, ConsoleColor? color=null) {
        ConsoleColor currentColor = Console.ForegroundColor;
        Console.ForegroundColor = color ?? currentColor;
        Console.WriteLine(msg);
        Console.ForegroundColor = currentColor;
    }
}
