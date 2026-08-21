using System;
using System.Threading.Tasks;
using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("JU")]
class JoinUserHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject)
    {
        int targetClientId = 0;
        try {
            var val = receivedObject.Get<NetworkObject>("p").Get<object>("0");
            targetClientId = Convert.ToInt32(val);
        } catch {}

        if (targetClientId > 0) {
            Client targetClient = null;
            foreach (var room in Room.AllRooms()) {
                foreach (var c in room.Clients) {
                    if (c.ClientID == targetClientId) {
                        targetClient = c;
                        break;
                    }
                }
                if (targetClient != null) break;
            }

            if (targetClient != null && targetClient.Room != null) {
                client.SetRoom(targetClient.Room);
            }
        }
        return Task.CompletedTask;
    }
}
