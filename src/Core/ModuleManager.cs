using System.Reflection;
using sodoffmmo.Attributes;

namespace sodoffmmo.Core;

class ModuleManager {
    Dictionary<int, Type> handlers = new();
    Dictionary<string, Type> extHandlers = new();

    public void RegisterModules() {
        RegisterHandlers();
        RegisterExtensionHandlers();
    }

    public CommandHandler GetCommandHandler(int id) {
        if (handlers.TryGetValue(id, out Type? handler))
            return (CommandHandler)Activator.CreateInstance(handler)!;
        throw new Exception($"Command handler with ID {id} not found!");
    }

    public CommandHandler GetCommandHandler(string name) {
        if (extHandlers.TryGetValue(name, out Type? handler))
            return (CommandHandler)Activator.CreateInstance(handler)!;
        throw new Exception($"Command handler with name \"{name}\" not found!");
    }

    private void RegisterHandlers() {
        handlers.Clear();
        var handlerTypes = Assembly.GetExecutingAssembly().GetTypes()
                            .Where(type => typeof(CommandHandler).IsAssignableFrom(type))
                            .Where(type => type.GetCustomAttribute<CommandHandlerAttribute>() != null);
        
        foreach (var handlerType in handlerTypes) {
            CommandHandlerAttribute attrib = (handlerType.GetCustomAttribute(typeof(CommandHandlerAttribute)) as CommandHandlerAttribute)!;
            handlers[attrib.ID] = handlerType;
        }
    }

    private void RegisterExtensionHandlers() {
        extHandlers.Clear();
        var extHandlerTypes = Assembly.GetExecutingAssembly().GetTypes()
                                .Where(type => typeof(CommandHandler).IsAssignableFrom(type))
                                .Where(type => type.GetCustomAttribute<ExtensionCommandHandlerAttribute>() != null);

        foreach (var extHandlerType in extHandlerTypes) {
            ExtensionCommandHandlerAttribute attrib = (extHandlerType.GetCustomAttribute(typeof(ExtensionCommandHandlerAttribute)) as ExtensionCommandHandlerAttribute)!;
            extHandlers[attrib.Name] = extHandlerType;
        }
    }
}