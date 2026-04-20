using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace sodoffmmo.Core;
internal static class Configuration {

    public static ServerConfiguration ServerConfiguration { get; private set; } = new ServerConfiguration();

    public static void Initialize() {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .Build();

        ServerConfiguration? serverConfiguration = config.GetSection("MMOServer").Get<ServerConfiguration>();
        if (serverConfiguration is null)
            return;

        ServerConfiguration = serverConfiguration;
        if (string.IsNullOrEmpty(ServerConfiguration.ApiUrl)) {
            ServerConfiguration.Authentication = AuthenticationMode.Disabled;
        }
    }
}

internal sealed class ServerConfiguration {
    public string ListenIP { get; set; } = string.Empty;
    public int Port { get; set; } = 9933;
    public string EventName { get; set; } = "ScoutAttack";
    public int EventTimer { get; set; } = 30;
    public int FirstEventTimer { get; set; } = 10;
    public Dictionary<string, string[][]> RoomAlerts { get; set; } = new();
    public Dictionary<string, AmbassadorConfiguration> AmbassadorRooms { get; set; } = new();
    public int RacingMaxPlayers { get; set; } = 6;
    public int RacingMinPlayers { get; set; } = 2;
    public int RacingMainLobbyTimer { get; set; } = 15;
    public int PingDelay { get; set; } = 17;
    public bool EnableChat { get; set; } = true;
    public bool EnableCannedChat { get; set; } = true;
    public bool AllowChaos { get; set; } = false;
    public AuthenticationMode Authentication { get; set; } = AuthenticationMode.Disabled;
    public string ApiUrl { get; set; } = "";
    public string BypassToken { get; set; } = "";
}

public enum AuthenticationMode {
    Disabled, Optional, RequiredForChat, Required
}

public class AmbassadorConfiguration {
    public int GaugeStart { get; set; } = 75;
    public int GaugeMax { get; set; } = 100;
    public float GaugeDecayRate { get; set; } = 60;
    public bool GaugeDecayOnlyWhenInRoom { get; set; } = true;
    public float GaugePlayers { get; set; } = 0.5f;
    public bool ResetOnMaxReached { get; set; } = false;
}