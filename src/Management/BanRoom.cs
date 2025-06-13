using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.Management;
public class BanRoom : Room {
    public BanRoom(string name) : base(name, name) {}

    public void UpdateClient(Client client) {
        client.Send(RespondJoinRoom());
    }

    public override void Send(NetworkPacket packet, Client? skip = null) {
        // Do nothing.
    }

    public override NetworkPacket RespondJoinRoom() {
        NetworkObject obj = new();
        NetworkArray roomInfo = new();
        roomInfo.Add(Id);
        roomInfo.Add(Name); // Room Name
        roomInfo.Add(Group); // Group Name
        roomInfo.Add(true); // is game
        roomInfo.Add(false); // is hidden
        roomInfo.Add(false); // is password protected
        roomInfo.Add((short)0); // player count
        roomInfo.Add((short)1); // max player count
        roomInfo.Add(new NetworkArray()); // empty roomvars
        roomInfo.Add((short)0); // spectator count
        roomInfo.Add((short)0); // max spectator count

        obj.Add("r", roomInfo);
        obj.Add("ul", new NetworkArray()); // Empty userlist.

        NetworkPacket packet = NetworkObject.WrapObject(0, 4, obj).Serialize();
        packet.Compress();

        return packet;
    }

    public override NetworkPacket SubscribeRoom() {
        NetworkObject obj = new();
        NetworkArray list = new();

        NetworkArray r1 = new();
        r1.Add(Id);
        r1.Add(Name); // Room Name
        r1.Add(Group); // Group Name
        r1.Add(true);
        r1.Add(false);
        r1.Add(false);
        r1.Add((short)0); // player count
        r1.Add((short)1); // max player count
        r1.Add(new NetworkArray());
        r1.Add((short)0);
        r1.Add((short)0);

        list.Add(r1);

        obj.Add("rl", list);
        obj.Add("g", Group);
        return NetworkObject.WrapObject(0, 15, obj).Serialize();
    }
}