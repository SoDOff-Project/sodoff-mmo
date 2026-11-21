using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Module;
using System;
using System.Collections.Generic;
using System.Text;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("createdragon", Role.Admin)]
class CreateDragonCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length != 1) {
            client.Send(Utils.BuildServerSideMessage("Invalid number of arguments", "Server"));
            return;
        }
        if (SimulatedPlayer.SimulatedPlayers.ContainsKey(arguments[0])) {
            client.Send(Utils.BuildServerSideMessage("Simulated dragon with this name already exists!", "Server"));
            return;
        }

        SimulatedPlayer player = new(client);
        SimulatedPlayer.SimulatedPlayers.Add(arguments[0], player);
        _ = player.InitSimulatedPlayer();
    }
}
