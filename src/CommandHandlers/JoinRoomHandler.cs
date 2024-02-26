using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("JA")]
class JoinRoomHandler : ICommandHandler
{
    Room room;
    Client client;
    public void Handle(Client client, NetworkObject receivedObject)
    {
        string roomName = receivedObject.Get<NetworkObject>("p").Get<string>("rn");
        client.LeaveRoom();
        client.InvalidatePlayerData();
        room = Room.GetOrAdd(roomName);
        Console.WriteLine($"Join Room: {roomName} RoomID: {room.Id} IID: {client.ClientID}");
        this.client = client;

        client.Send(room.RespondJoinRoom());
        client.Send(room.SubscribeRoom());
        UpdatePlayerUserVariables();
        client.Room = room;
    }

    private void UpdatePlayerUserVariables() {
        foreach (Client c in room.Clients) {
            NetworkObject cmd = new();
            NetworkObject obj = new();
            cmd.Add("c", "SUV");
            obj.Add("MID", c.ClientID);
            cmd.Add("p", obj);
            client.Send(NetworkObject.WrapObject(1, 13, cmd).Serialize());
        }

    }
}
