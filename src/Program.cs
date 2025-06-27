using sodoffmmo;
using sodoffmmo.Core;
using System.Net;
using System.IO;
using System.Reflection;

Configuration.Initialize();

string pluginsDir = Path.GetFullPath("plugins");
Console.WriteLine($"Loading plugins from '{pluginsDir}' ...");
List<dynamic> plugins = new();
try {
    foreach (var dllFile in Directory.GetFiles(pluginsDir, "*.dll")) {
        var dll = Assembly.LoadFrom(dllFile);
        foreach(Type type in dll.GetExportedTypes()) {
            if (type.Namespace == "SoDOff_MMO_Plugin") {
                Console.WriteLine($" * {type} ({Path.GetFileName(dllFile)})");
                plugins.Add(Activator.CreateInstance(type));
            }
        }
    }
} catch (System.IO.DirectoryNotFoundException) {
    Console.WriteLine(" - Skipped: plugins directory do not exist");
}

Console.WriteLine($"Starting MMO server ...");
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
