using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;
using System.Text;
using System;

namespace sodoffmmo.CommandHandlers;

[CommandHandler(0)]
class HandshakeHandler : CommandHandler
{
    public override Task Handle(Client client, NetworkObject receivedObject)
    {
        string? token = receivedObject.Get<string>("rt");
        if (token != null) {
            client.Send(NetworkObject.WrapObject(0, 1006, new NetworkObject()).Serialize());
            return Task.CompletedTask;
        }

        string? api = receivedObject.Get<string>("api");
        if (api != null && api[0] == '0') {
            client.OldApi = true;
        }

        NetworkObject obj = new();

        obj.Add("tk", RandomString(32));
        obj.Add("ct", 1024);
        obj.Add("ms", 1000000);

        client.Send(NetworkObject.WrapObject(0, 0, obj).Serialize());
        return Task.CompletedTask;
    }

    private string RandomString(int length) {
        Random random = new Random();
        const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
        var builder = new StringBuilder();

        for (var i = 0; i < length; i++) {
            var c = pool[random.Next(0, pool.Length)];
            builder.Append(c);
        }

        return builder.ToString();
    }
}
