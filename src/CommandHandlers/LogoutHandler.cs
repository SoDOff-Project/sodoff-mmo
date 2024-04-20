using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[CommandHandler(2)]
class LogoutHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject)
    {
        client.SetRoom(null);
        client.PlayerData.UNToken = null;
        client.PlayerData.ZoneName = null;

        NetworkObject content = new();
        content.Add("zn", "");

        client.Send(NetworkObject.WrapObject(0, 2, content).Serialize());
        return Task.CompletedTask;
    }
}
