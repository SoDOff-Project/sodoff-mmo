using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("bypass", Role.Admin)]
class BypassCommand : IManagementCommand
{
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length == 0) {
            client.Send(Utils.BuildServerSideMessage("Bypass: No message to send", "Server"));
            return;
        }

        string message = string.Join(' ', arguments);
        client.Room.Send(Utils.BuildChatMessage(client.PlayerData.Uid, message, client.PlayerData.DiplayName), client);

        NetworkObject cmd = new();
        NetworkObject data = new();
        data.Add("arr", new string[] { "SCA", "-1", "1", message, "", "1" });
        cmd.Add("c", "SCA");
        cmd.Add("p", data);
        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Send(packet);
    }
}
