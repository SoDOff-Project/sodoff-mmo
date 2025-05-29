using Discord.WebSocket;
using Discord;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using sodoffmmo.Core;
using Discord.Rest;
using sodoffmmo.Data;

namespace sodoffmmo.Management;

class DiscordManager {
    private static readonly string token_file;

    private static readonly ConcurrentQueue<QueuedMessage> queue = new();
    private static DiscordSocketClient client;
    private static BotConfig config;
    private static SocketTextChannel channel;

    static DiscordManager() {
        DirectoryInfo dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "src"));
        while (dir.Parent != null) {
            if (dir.Name == "src" && dir.Exists) break;
            dir = dir.Parent;
        }
        if (dir.Parent == null) dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        token_file = Path.Combine(dir.FullName, "discord_bot.json");
    }

    public static void Initialize() {
        try {
            // This approach is taken because I don't want to accidentally
            // push my bot token because it was in appsettings. The
            // json handled here has been added to gitignore.
            if (File.Exists(token_file)) {
                Task.Run(() => RunBot(JsonSerializer.Deserialize<BotConfig>(File.ReadAllText(token_file))));
            } else {
                Log("Discord Integration not started because the file containing the token was not found.\nPut the token in \""+token_file+"\" and restart the server to use Discord Integration.", ConsoleColor.Yellow);
                File.WriteAllText(token_file, JsonSerializer.Serialize(new BotConfig {
                    Token = ""
                }, new JsonSerializerOptions {
                    WriteIndented = true
                }));
            }
        } catch (Exception e) {
            Log(e.ToString(), ConsoleColor.Red);
        }
    }

    private static async Task RunBot(BotConfig? config) {
        if (config == null) {
            Log("Bot config didn't deserialize correctly. Discord Integration is not active.", ConsoleColor.Yellow);
            return;
        }
        client = new DiscordSocketClient(new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent,
            AlwaysDownloadUsers = true
        });
        client.Ready += async () => {
            channel = client.GetGuild(config.Server).GetTextChannel(config.Channel);
        };
        client.MessageReceived += OnDiscordMessage;
        await client.LoginAsync(TokenType.Bot, config.Token);
        DiscordManager.config = config;
        Log("Loaded bot config from "+token_file+" and bot is running.");
        await client.StartAsync();
        while (true) {
            try {
                if (!queue.IsEmpty && queue.TryDequeue(out QueuedMessage message)) {
                    SocketTextChannel? destination = channel;
                    if (message.room != null) {
                        if (message.room.Name == "LIMBO") continue;
                        string room = message.room.Name;
                        destination = channel.Threads.SingleOrDefault(x => x.Name == room);
                        if (destination == null) {
                            // A discord channel can have up to 1000 active threads
                            // and an unlimited number of archived threads.
                            if (message.room.AutoRemove) {
                                // Set temporary rooms (such as user rooms) to shorter archive times.
                                destination = await channel.CreateThreadAsync(room, autoArchiveDuration: ThreadArchiveDuration.OneHour);
                            } else {
                                // Persistent rooms should stay around for longer.
                                destination = await channel.CreateThreadAsync(room, autoArchiveDuration: ThreadArchiveDuration.OneWeek);
                            }
                        }
                    }
                    await destination.SendMessageAsync(message.msg);
                }
            } catch (Exception e) {
                Log(e.ToString(), ConsoleColor.Red);
            }
        }
    }

    // For handling commands sent directly in the channel.
    private static async Task OnDiscordMessage(SocketMessage arg) {
        // Check that message is from a user in the correct place.
        bool roomChannel = arg.Channel is SocketThreadChannel thr && thr.ParentChannel.Id == config.Channel;
        if (!(
                arg.Channel.Id == config.Channel ||
                roomChannel
            ) ||
            arg is not SocketUserMessage socketMessage ||
            socketMessage.Author.IsBot
        ) return;
        
        string[] message = socketMessage.Content.Split(' ');
        if (message.Length >= 1) {
            string[] everywhereCommands = new string[] {
                // Denotes commands that work identically regardless
                // of whether you're in a room thread.
                "!tempmute <int: vikingid> [string... reason] - Mute someone until they reconnect."
            };
            bool un_command = message[0].StartsWith("!un");
            if (message[0] == "!tempmute" || message[0] == "!untempmute") {
                if (message.Length > 1 && int.TryParse(message[1], out int id)) {
                    foreach (Room room in Room.AllRooms()) {
                        foreach (Client player in room.Clients) {
                            if (player.PlayerData.VikingId == id) {
                                if (player.TempMuted != un_command) {
                                    await arg.Channel.SendMessageAsync($"This player is {(un_command ? "not" : "already")} tempmuted.", messageReference: arg.Reference);
                                } else {
                                    player.TempMuted = !un_command;
                                    string reason = string.Empty;
                                    string msg = un_command ? "Un-tempmuted" : "Tempmuted";
                                    msg += $" {player.PlayerData.DiplayName}.";
                                    if (message.Length >= 2) {
                                        reason = string.Join(' ', message, 2, message.Length - 2);
                                        msg += " Reason: "+reason;
                                    }
                                    await arg.Channel.SendMessageAsync(msg, messageReference: arg.Reference);
                                }
                                goto tempmute_search_loop_end;
                            }
                        }
                    }
                    tempmute_search_loop_end:;// StackOverflow said to do it like this (no labeled break like in Java).
                } else {
                    await arg.Channel.SendMessageAsync("Input a valid vikingid!", messageReference: arg.Reference);
                }
            } else if (roomChannel) {
                if (message[0] == "!help") {
                    await arg.Channel.SendMessageAsync(string.Join(Environment.NewLine, new string[] {
                            "### List of commands:",
                            "!help - Prints the help message. That's this message right here!",
                            "!list - Lists all players in this room.",
                            "!say <string... message> - Say something in this room."
                        }.Concat(everywhereCommands)
                    ));
                } else if (message[0] == "!list") {
                    if (!Room.Exists(arg.Channel.Name)) {
                        await arg.Channel.SendMessageAsync("This room is currently unloaded.", messageReference: arg.Reference);
                    } else {
                        Room room = Room.Get(arg.Channel.Name);
                        if (room.ClientsCount > 0) {
                            string msg = "Players in Room:\n";
                            foreach (Client player in room.Clients) {
                                string vikingid;
                                if (player.PlayerData.VikingId != 0) {
                                    vikingid = "||VikingId: "+player.PlayerData.VikingId+"||";
                                } else {
                                    vikingid = "||(Not Authenticated!)||";
                                }
                                msg += $"* {SanitizeString(player.PlayerData.DiplayName)} {vikingid}\n";
                            }
                            SendMessage(msg, room); // Sent like this in case it's longer than 2000 characters (handled by this method).
                        } else {
                            await arg.Channel.SendMessageAsync("This room is empty.", messageReference: arg.Reference);
                        }
                    }
                } else if (message[0] == "!say") {
                    if (message.Length > 1) {
                        // Due to client-based restrictions, this probably only works in SoD and maybe MaM.
                        // Unless we want to create an MMOAvatar instance for the server "user".
                        if (Room.Exists(arg.Channel.Name)) {
                            Room room = Room.Get(arg.Channel.Name);
                            if (room.ClientsCount > 0) {
                                room.Send(Utils.BuildChatMessage(Guid.Empty.ToString(), string.Join(' ', message, 1, message.Length-1), "Server"));
                                await arg.AddReactionAsync(Emote.Parse(":white_check_mark:"));
                            } else {
                                await arg.Channel.SendMessageAsync("There is no-one in this room to see your message.", messageReference: arg.Reference);
                            }
                        } else {
                            await arg.Channel.SendMessageAsync("This room is currently unloaded.", messageReference: arg.Reference);
                        }
                    } else {
                        await arg.Channel.SendMessageAsync("You didn't include any message content!", messageReference: arg.Reference);
                    }
                }
            } else {
                if (message[0] == "!help") {
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
                    IEnumerable<RestThreadChannel> threads = await channel.GetActiveThreadsAsync();
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
                    string msg = string.Empty;
                    foreach (Room room in Room.AllRooms()) {
                        foreach (Client player in room.Clients) {
                            if (msg == string.Empty) msg = "Players Online:\n";
                            
                            string vikingid;
                            if (player.PlayerData.VikingId != 0) {
                                vikingid = "||VikingId: "+player.PlayerData.VikingId+"||";
                            } else {
                                vikingid = "||(Not Authenticated!)||";
                            }
                            msg += $"* {SanitizeString(player.PlayerData.DiplayName)} {vikingid} ({room.Name})\n";
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
    public static void SendMessage(string msg, Room? room=null) {
        if (client != null) {
            while (msg.Length > 0) {
                string piece = msg;
                if (msg.Length > 2000) { // Discord character limit is 2000.
                    // Find a good place to break the message, if possible.
                    int breakIndex = msg.LastIndexOfAny(new char[2] {' ','\n'}, 2000);
                    if (breakIndex <= 0) breakIndex = 2000;
                    // Split the first part of the message and recycle the rest.
                    piece = msg.Substring(0, breakIndex);
                    msg = msg.Substring(breakIndex);
                } else {
                    // Exit the loop when the time comes.
                    msg = string.Empty;
                }
                queue.Enqueue(new QueuedMessage {
                    msg = piece,
                    room = room
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
            if (client != null) {
                // We need to sanitize things first so that people can't
                // use Discord's formatting features inside a chat message.
                msg = SanitizeString(msg);
                string name = SanitizeString(player?.DiplayName ?? "");

                if (player != null) {
                    if (player.VikingId != 0) {
                        surround = "||VikingId: "+player.VikingId+"||\n"+surround;
                    } else {
                        surround = "||(Not Authenticated!)||\n"+surround;
                    }
                }

                // Send the sanitized message, headed with the viking id (if authenticated).
                SendMessage(string.Format(surround, msg, name), room);
            }
        } catch (Exception e) {
            Log(e.ToString(), ConsoleColor.Red);
        }
    }

    public static string SanitizeString(string str) {
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

    class BotConfig {
        [JsonPropertyName("// Token")]
        private string __TokenComment { get; set; } = "This is your bot's token. DO NOT SHARE THIS WITH ANYBODY!!!";
        public string Token { get; set; }

        [JsonPropertyName("// Server")]
        private string __ServerComment { get; set; } = "This is the Server ID that the bot connects to.";
        public ulong Server { get; set; }

        [JsonPropertyName("// Channel")]
        private string __ChannelComment { get; set; } = "This is the Channel ID where bot logs things and can run admin commands from.";
        public ulong Channel { get; set; }
    }

    private class QueuedMessage {
        public string msg;
        public Room? room;
    }

    private static void Log(string msg, ConsoleColor? color=null) {
        ConsoleColor currentColor = Console.ForegroundColor;
        Console.ForegroundColor = color ?? currentColor;
        Console.WriteLine(msg);
        Console.ForegroundColor = currentColor;
    }
}
