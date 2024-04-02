using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("JA")]
class JoinRoomHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject)
    {
        string roomName = receivedObject.Get<NetworkObject>("p").Get<string>("rn");
        Room room = Room.GetOrAdd(roomName);
        client.SetRoom(room);
        return Task.CompletedTask;
    }
}
