﻿using System;
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

        ServerConfiguration? serverConfiguration = config.GetSection("ServerConfiguration").Get<ServerConfiguration>();
        if (serverConfiguration is null)
            return;

        ServerConfiguration = serverConfiguration;
    }
}

internal sealed class ServerConfiguration {
    public string ListenIP { get; set; } = string.Empty;
    public int Port { get; set; } = 9933;
    public int EventTimer { get; set; } = 30;
    public int FirstEventTimer { get; set; } = 10;
    public int RacingMaxPlayers { get; set; } = 6;
    public int RacingMinPlayers { get; set; } = 2;
    public int RacingMainLobbyTimer { get; set; } = 15;
    public bool EnableChat { get; set; } = true;
    public bool AllowChaos { get; set; } = false;
}
