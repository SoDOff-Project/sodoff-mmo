using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("PNG")]
class PingHandler : CommandHandler {
    public bool RunInBackground { get; } = true;

    public override async Task Handle(Client client, NetworkObject receivedObject) {
        if (Configuration.ServerConfiguration.PingDelay > 0) {
            await Task.Delay(Configuration.ServerConfiguration.PingDelay);
        }
        NetworkObject cmd = new();
        NetworkObject obj = new();
        obj.Add("arr", new string[] { "PNG", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() });
        cmd.Add("c", "PNG");
        cmd.Add("p", obj);
        client.Send(NetworkObject.WrapObject(1, 13, cmd).Serialize());
    }
}
