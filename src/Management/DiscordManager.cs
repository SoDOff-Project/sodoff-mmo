using Discord.WebSocket;
using Discord;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using sodoffmmo.Core;

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
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });
        client.Ready += OnBotReady;
        client.MessageReceived += OnDiscordMessage;
        await client.LoginAsync(TokenType.Bot, config.Token);
        DiscordManager.config = config;
        Log("Loaded bot config from "+token_file+" and bot is running.");
        await client.StartAsync();
        while (true) {
            if (queue.TryDequeue(out QueuedMessage message)) {
                SocketTextChannel? destination = channel;
                if (message.room != null) {
                    string room = message.room.Name;
                    destination = channel.Threads.SingleOrDefault(x => x.Name == room);
                    if (destination == null) {
                        // A discord channel can have up to 1000 active threads
                        // and an unlimited number of archived threads.
                        if (message.room.AutoRemove) {
                            // Set temporary rooms (such as user rooms) to shorter archive times.
                            destination = await channel.CreateThreadAsync(room, autoArchiveDuration: ThreadArchiveDuration.OneDay);
                        } else {
                            // Persistent rooms should stay around for longer.
                            destination = await channel.CreateThreadAsync(room, autoArchiveDuration: ThreadArchiveDuration.OneWeek);
                        }
                        foreach (SocketGuildUser? user in channel.Users) {
                            await ((SocketThreadChannel)destination).AddUserAsync(user);
                        }
                    }
                }
                await destination.SendMessageAsync(message.msg);
            }
        }
    }

    private static async Task OnBotReady() {
        channel = client.GetGuild(config.Server).GetTextChannel(config.Channel);
    }

    // For handling commands sent directly in the channel.
    private static async Task OnDiscordMessage(SocketMessage arg) {
        // Check that message is from a user in the correct place.
        if (arg.Channel.Id != config.Channel || arg is not SocketUserMessage socketMessage || socketMessage.Author.IsBot) return;
        
        string[] message = socketMessage.Content.Split(' ');
        if (message.Length >= 1) {
            if (message[0] == "help") {
                SendMessage("No-one can help you.");
            }
        }
    }

    public static void SendMessage(string msg, Room? room=null) {
        if (client != null) queue.Enqueue(new QueuedMessage {
            msg = msg
                .Replace("*", "\\*")   // Prevent most Discord formatting
                .Replace("_", "\\_")   // Prevent some other formatting
                .Replace("~", "\\~")   // Prevent Strikethrough
                .Replace("`", "\\`")   // Prevent Code Blocks
                .Replace(":", "\\:")   // Prevent Links
                .Replace("<", "\\<")   // So that Masked Links with embed disabled don't look strange
                .Replace("](", "]\\("), // Prevent Masked Links
            room = room
        });
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
