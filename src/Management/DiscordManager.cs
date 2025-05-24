using Discord.WebSocket;
using Discord;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace sodoffmmo.Management;

class DiscordManager {
    private static readonly string token_file;

    private static DiscordSocketClient client;
    private static BotConfig config;

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
        client.MessageReceived += OnMessage;
        await client.LoginAsync(TokenType.Bot, config.Token);
        DiscordManager.config = config;
        Log("Loaded bot config from "+token_file+" and bot is running.");
        await client.StartAsync();
    }
    private static async Task OnMessage(SocketMessage arg) {
        // Check that message is from a user in the correct place.
        if (arg is not SocketUserMessage socketMessage || socketMessage.Author.IsBot || socketMessage.Channel.Id != config.Channel) return;
        
        string[] message = socketMessage.Content.Split(' ');
        if (message.Length >= 1) {
            if (message[0] == "test") {
                await socketMessage.Channel.SendMessageAsync("fungus amongus");
            }
        }
    }

    class BotConfig {
        [JsonPropertyName("// Token")]
        private string __TokenComment { get{
                return "This is your bot's token. DO NOT SHARE THIS WITH ANYBODY!!!";
        } set {}}
        public string Token { get; set; }

        [JsonPropertyName("// Channel")]
        private string __ChannelComment { get{
                return "This is the Channel ID where bot logs things and can run admin commands from.";
        } set {}}
        public ulong Channel { get; set; }
    }

    private static void Log(string msg, ConsoleColor? color=null) {
        ConsoleColor currentColor = Console.ForegroundColor;
        Console.ForegroundColor = color ?? currentColor;
        Console.WriteLine(msg);
        Console.ForegroundColor = currentColor;
    }
}
