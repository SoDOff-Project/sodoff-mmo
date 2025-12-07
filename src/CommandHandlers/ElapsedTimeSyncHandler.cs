using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("RTM")]
class ElapsedTimeSyncHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        if (client.Room != null) {
            NetworkObject cmd = new();
            NetworkObject obj = new();
            obj.Add("arr", new string[] { "RTM", "-1", Runtime.CurrentRuntime.ToString() });
            cmd.Add("c", "RTM");
            cmd.Add("p", obj);
            client.Send(NetworkObject.WrapObject(1, 13, cmd).Serialize());
        }
        return Task.CompletedTask;
    }
}