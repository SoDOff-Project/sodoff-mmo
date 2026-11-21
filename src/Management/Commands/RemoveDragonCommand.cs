using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Module;
using System;
using System.Collections.Generic;
using System.Text;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("removedragon", Role.Admin)]
class RemoveDragonCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length != 1) {
            client.Send(Utils.BuildServerSideMessage("Invalid number of arguments", "Server"));
            return;
        }
        if (!SimulatedPlayer.SimulatedPlayers.ContainsKey(arguments[0])) {
            client.Send(Utils.BuildServerSideMessage("Simulated dragon with this name doesn't exist!", "Server"));
            return;
        }
        SimulatedPlayer.SimulatedPlayers[arguments[0]].Destroy();
        SimulatedPlayer.SimulatedPlayers.Remove(arguments[0]);
    }
}
