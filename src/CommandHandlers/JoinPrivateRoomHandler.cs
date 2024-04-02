using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("JO")]
class JoinPrivateRoomHandler : ICommandHandler
{
    public void Handle(Client client, NetworkObject receivedObject)
    {
        var p = receivedObject.Get<NetworkObject>("p");
        string roomName = p.Get<string>("rn") + "_" + p.Get<string>("0");
        Room room = Room.GetOrAdd(roomName, autoRemove: true);
        client.SetRoom(room);
    }
}
