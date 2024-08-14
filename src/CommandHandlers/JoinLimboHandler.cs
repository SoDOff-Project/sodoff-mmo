using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers
{
    [ExtensionCommandHandler("JL")]
    public class JoinLimboHandler : CommandHandler
    {
        public override Task Handle(Client client, NetworkObject receivedObject)
        {
            // set client room to limbo
            client.SetRoom(Room.GetOrAdd("LIMBO"));

            return Task.CompletedTask;
        }
    }
}
