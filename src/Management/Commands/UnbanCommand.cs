using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("unban", Role.Moderator)]
class UnbanCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length < 1) {
            client.Send(Utils.BuildServerSideMessage("Unban: Invalid number of arguments", "Server"));
            return;
        }
        string name = string.Join(' ', arguments);
        int? id = null;
        if (PunishmentManager.BannedList.ContainsValue(name)) // Find in list in case they're offline.
            id = PunishmentManager.BannedList.FirstOrDefault(p => p.Value == name).Key;
        
        if (id == null) { // Failure
            client.Send(Utils.BuildServerSideMessage($"Unban: User named \"{name}\" is not banned", "Server"));
            return;
        }

        Client? target = PunishmentManager.BanRoom.Clients.FirstOrDefault(x => x.PlayerData.VikingId == id || x.PlayerData.DiplayName == name); // Find in ban room.

        if (target.Unban(id)) {
            client.Send(Utils.BuildServerSideMessage($"Unban: {name} has been unbanned", "Server"));
        } else {
            client.Send(Utils.BuildServerSideMessage($"Unban: {name} is not banned", "Server"));
        }
    }
}