using sodoffmmo.Attributes;
using sodoffmmo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace sodoffmmo.Management;
public class ManagementCommandProcessor {
    static Dictionary<Tuple<string, Role>, Type> commands = new();
    static bool initialized = false;

    public static void Initialize() {
        if (Configuration.ServerConfiguration.Authentication == AuthenticationMode.Disabled)
            return;
        commands.Clear();
        var handlerTypes = Assembly.GetExecutingAssembly().GetTypes()
                            .Where(type => typeof(IManagementCommand).IsAssignableFrom(type))
                            .Where(type => type.GetCustomAttribute<ManagementCommandAttribute>() != null);

        foreach (var handlerType in handlerTypes) {
            ManagementCommandAttribute attrib = (handlerType.GetCustomAttribute(typeof(ManagementCommandAttribute)) as ManagementCommandAttribute)!;
            commands[new Tuple<string, Role>(attrib.Name, attrib.Role)] = handlerType;
        }
        initialized = true;
    }

    public static bool ProcessCommand(string message, Client client) {
        if (!initialized)
            return false;
        if (!message.StartsWith("::") || message.Length < 3)
            return false;

        string[] parts = message.Split(' ');
        string commandName = parts[0].Substring(2);
        string[] arguments = parts.Skip(1).ToArray();

        for (int i = (int)client.PlayerData.Role; i >= 0; --i) {
            Role currentRole = (Role)i;

            if (commands.TryGetValue(new Tuple<string, Role>(commandName, currentRole), out Type? commandType)) {
                IManagementCommand command = (IManagementCommand)Activator.CreateInstance(commandType)!;
                Console.WriteLine($"Management command {commandName} by {client.PlayerData.DiplayName} ({client.PlayerData.Uid}) in {client.Room.Name}");
                command.Handle(client, arguments);
                return true;
            }
        }
        return false;
    }
}
