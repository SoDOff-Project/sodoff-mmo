using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Module;
using System;
using System.Collections.Generic;
using System.Text;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("listdragons", Role.Admin)]
class ListDragonsCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        client.Send(Utils.BuildServerSideMessage(string.Join(", ", SimulatedPlayer.SimulatedPlayers.Keys), "Server"));
    }
}
