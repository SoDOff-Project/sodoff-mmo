using sodoffmmo.Data;

namespace sodoffmmo.Core;
public abstract class CommandHandler {
    public bool RunInBackground { get; }

    public abstract Task Handle(Client client, NetworkObject receivedObject);
}
