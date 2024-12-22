using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("PM")]
class RacingPMHandler : CommandHandler
{
    // rec: {"a":13,"c":1,"p":{"c":"PM","p":{"M":"DT:c4647597-a72a-4f34-973c-5a10218d9a64:1000","en":"we"},"r":-1}}
    public override Task Handle(Client client, NetworkObject receivedObject) {
        // send: {"a":13,"c":1,"p":{"c":"PM","p":{"arr":[{"M":["DT:f05fc387-7358-4bff-be04-7c316f0a8de8:1000"],"MID":3529441}]}}}
        NetworkObject cmd = new();
        NetworkObject p = new();
        NetworkArray arr = new();
        NetworkObject data = new();
        string M = receivedObject.Get<NetworkObject>("p").Get<string>("M");
        if (M.StartsWith("WF:") || M.StartsWith("WFWD:")) {
            // When firing weapon in EMD, recieving clients expect userid, but the sending client sends its token instead.
            string token = M.Split(':')[1];
            M = M.Replace(token, client.PlayerData.Uid);
        }
        data.Add("M", new string[] {M});
        data.Add("MID", client.ClientID);
        arr.Add(data);
        p.Add("arr", arr);
        cmd.Add("c", "PM");
        cmd.Add("p", p);
        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();

        if (client.Room != null) // Throws an exception in Eat my Dust when the player fires their weapon before fully in the room.
            client.Room.Send(packet);
        return Task.CompletedTask;
    }
}
