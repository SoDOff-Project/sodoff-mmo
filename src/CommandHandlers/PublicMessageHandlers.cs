using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("PM")]
class RacingPMHandler : ICommandHandler
{
    // rec: {"a":13,"c":1,"p":{"c":"PM","p":{"M":"DT:c4647597-a72a-4f34-973c-5a10218d9a64:1000","en":"we"},"r":-1}}
    public void Handle(Client client, NetworkObject receivedObject) {
        // send: {"a":13,"c":1,"p":{"c":"PM","p":{"arr":[{"M":["DT:f05fc387-7358-4bff-be04-7c316f0a8de8:1000"],"MID":3529441}]}}}
        NetworkObject cmd = new();
        NetworkObject p = new();
        NetworkArray arr = new();
        NetworkObject data = new();
        data.Add("M", new string[] {
            receivedObject.Get<NetworkObject>("p").Get<string>("M")
        });
        arr.Add(data);
        p.Add("arr", arr);
        p.Add("MID", client.ClientID);
        cmd.Add("c", "PM");
        cmd.Add("p", p);
        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();

        foreach (var roomClient in client.Room.Clients) {
            roomClient.Send(packet);
        }
    }
}
