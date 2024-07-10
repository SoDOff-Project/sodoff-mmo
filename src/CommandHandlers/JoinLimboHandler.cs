using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sodoffmmo.CommandHandlers
{
    [ExtensionCommandHandler("JL")]
    public class JoinLimboHandler : CommandHandler
    {
        public override Task Handle(Client client, NetworkObject receivedObject)
        {
            // set client room to limbo
            Room limbo = Room.GetOrAdd("LIMBO");
            if (limbo != null) client.SetRoom(limbo);

            return Task.CompletedTask;
        }
    }
}
