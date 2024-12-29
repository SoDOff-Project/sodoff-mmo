using System.Globalization;
using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

// TODO: This is currently stubbed. Supposed to do something.
// Should probably be done by someone who actually played the game.
[ExtensionCommandHandler("SU")]
public class EMDZombiesUpdateHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        return Task.CompletedTask;
    }
}

[ExtensionCommandHandler("EN")]
public class EMDZombiesEnterHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        return Task.CompletedTask;
    }
}

[ExtensionCommandHandler("EX")]
public class EMDZombiesExitHandler : CommandHandler {
    public override Task Handle(Client client, NetworkObject receivedObject) {
        return Task.CompletedTask;
    }
}
