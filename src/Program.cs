using sodoffmmo;
using sodoffmmo.Core;
using System.Net;

Configuration.Initialize();
Server server = new(IPAddress.Any, Configuration.ServerConfiguration.Port);
await server.Run();
