using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("DT")]
class DateTimeHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        NetworkObject cmd = new();
        NetworkObject obj = new();
        obj.Add("arr", new string[] { "DT", DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss") });
        cmd.Add("c", "DT");
        cmd.Add("p", obj);
        client.Send(NetworkObject.WrapObject(1, 13, cmd).Serialize());
        return Task.CompletedTask;
    }
}
