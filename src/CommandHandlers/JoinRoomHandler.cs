using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("JA")]
class JoinRoomHandler : ICommandHandler
{
    public void Handle(Client client, NetworkObject receivedObject)
    {
        string roomName = receivedObject.Get<NetworkObject>("p").Get<string>("rn");
        Room room = Room.GetOrAdd(roomName);
        Console.WriteLine($"Join Room: {roomName} RoomID: {room.Id} IID: {client.ClientID}");
        client.JoinRoom(room);
    }
}
