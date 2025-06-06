using sodoffmmo.Attributes;
using sodoffmmo.Core;
using System.Numerics;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("ban", Role.Moderator)]
class BanCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length < 1) {
            client.Send(Utils.BuildServerSideMessage("Ban: Invalid number of arguments", "Server"));
            return;
        }
        string name = string.Join(' ', arguments);
        Client? target = client.Room.Clients.FirstOrDefault(x => x.PlayerData.DiplayName == name); // Find in this room.
        if (target == null) { // Find through all the rooms.
            foreach (Room room in Room.AllRooms()) {
                if (room == client.Room) continue; // Skip; we already checked here.
                target = room.Clients.FirstOrDefault(x => x.PlayerData.DiplayName == name);
                if (target != null) break;
            }
        }
        if (target == null) { // Failure
            client.Send(Utils.BuildServerSideMessage($"Ban: user {name} not found", "Server"));
            return;
        }

        if (target.Banned) {
            client.Send(Utils.BuildServerSideMessage($"Ban: {name} is already banned", "Server"));
        } else {
            target.Banned = true;
            target.SetRoom(target.Room); // This will run the client through the 'if Banned' conditions.
            client.Send(Utils.BuildServerSideMessage($"Ban: {name} has been banned", "Server"));
            if (target.PlayerData.VikingId != null) {
                Client.BannedList.Add((int)target.PlayerData.VikingId, target.PlayerData.DiplayName);
                Client.SaveMutedBanned();
            } else {
                client.Send(Utils.BuildServerSideMessage($"Mute: This user is not authenticated. Their ban will not be permanent!", "Server"));

            }
        }
    }
}

[ManagementCommand("unban", Role.Moderator)]
class UnbanCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length < 1) {
            client.Send(Utils.BuildServerSideMessage("Unban: Invalid number of arguments", "Server"));
            return;
        }
        string name = string.Join(' ', arguments);
        int? id = null;
        if (Client.BannedList.ContainsValue(name)) id = Client.BannedList.FirstOrDefault(p => p.Value == name).Key; // Find in list in case they're offline.
        Client? target = client.Room.Clients.FirstOrDefault(x => x.PlayerData.VikingId == id || x.PlayerData.DiplayName == name); // Find in this room.
        if (target == null) { // Find through all the rooms.
            foreach (Room room in Room.AllRooms()) {
                if (room == client.Room) continue; // Skip; we already checked here.
                target = room.Clients.FirstOrDefault(x => x.PlayerData.VikingId == id || x.PlayerData.DiplayName == name);
                if (target != null) break;
            }
        }
        if (target == null && id == null) { // Failure
            client.Send(Utils.BuildServerSideMessage($"Unban: user {name} not found", "Server"));
            return;
        }

        if (target?.Muted() == false || (id != null && !Client.BannedList.ContainsKey((int)id))) {
            client.Send(Utils.BuildServerSideMessage($"Unban: {name} is not banned", "Server"));
        } else {
            if (target != null) {
                target.Banned = false;
                target.SetRoom(target.ReturnRoomOnPardon); // Put the target back.
            }
            client.Send(Utils.BuildServerSideMessage($"Unban: {name} has been unbanned", "Server"));
            if (id != null) {
                Client.BannedList.Remove((int)id);
                Client.SaveMutedBanned();
            }
        }
    }
}