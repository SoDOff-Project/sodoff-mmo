using sodoffmmo.Attributes;
using sodoffmmo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("playercount", Role.Admin)]
class PlayerCount : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        client.Send(Utils.BuildServerSideMessage($"Current room: {(client.Room?.ClientsCount.ToString() ?? "not in room")}, Server total: {Room.AllRooms().Sum(x => x.ClientsCount)}", "Server"));
    }
}
