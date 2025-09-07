using sodoffmmo;
using sodoffmmo.Core;
using System.Net;

Configuration.Initialize();
ChatFilter.Initialize();

Server server;

if (String.IsNullOrEmpty(Configuration.ServerConfiguration.ListenIP) || Configuration.ServerConfiguration.ListenIP == "*") {
    server = new(
        IPAddress.IPv6Any,
        Configuration.ServerConfiguration.Port,
        true
    );
} else {
    server = new(
        IPAddress.Parse(Configuration.ServerConfiguration.ListenIP),
        Configuration.ServerConfiguration.Port,
        false
    );
}
await server.Run();
