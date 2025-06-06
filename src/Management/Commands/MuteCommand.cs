using sodoffmmo.Attributes;
using sodoffmmo.Core;

namespace sodoffmmo.Management.Commands;

[ManagementCommand("mute", Role.Moderator)]
class MuteCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length < 1) {
            client.Send(Utils.BuildServerSideMessage("Mute: Invalid number of arguments", "Server"));
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
            client.Send(Utils.BuildServerSideMessage($"Mute: user {name} not found", "Server"));
            return;
        }

        if (target.RealMuted) {
            client.Send(Utils.BuildServerSideMessage($"Mute: {name} is already muted", "Server"));
        } else {
            target.RealMuted = true;
            client.Send(Utils.BuildServerSideMessage($"Mute: {name} has been muted", "Server"));
            if (target.PlayerData.VikingId != null) {
                Client.MutedList.Add((int)target.PlayerData.VikingId, target.PlayerData.DiplayName);
                Client.SaveMutedBanned();
            } else {
                client.Send(Utils.BuildServerSideMessage($"Mute: This user is not authenticated. Their mute will not be permanent!", "Server"));

            }
        }
    }
}

[ManagementCommand("unmute", Role.Moderator)]
class UnmuteCommand : IManagementCommand {
    public void Handle(Client client, string[] arguments) {
        if (arguments.Length < 1) {
            client.Send(Utils.BuildServerSideMessage("Unmute: Invalid number of arguments", "Server"));
            return;
        }
        string name = string.Join(' ', arguments);
        int? id = null;
        if (Client.MutedList.ContainsValue(name)) id = Client.MutedList.FirstOrDefault(p => p.Value == name).Key; // Find in list in case they're offline.
        Client? target = client.Room.Clients.FirstOrDefault(x => x.PlayerData.VikingId == id || x.PlayerData.DiplayName == name); // Find in this room.
        if (target == null) { // Find through all the rooms.
            foreach (Room room in Room.AllRooms()) {
                if (room == client.Room) continue; // Skip; we already checked here.
                target = room.Clients.FirstOrDefault(x => x.PlayerData.VikingId == id || x.PlayerData.DiplayName == name);
                if (target != null) break;
            }
        }
        if (target == null && id == null) { // Failure
            client.Send(Utils.BuildServerSideMessage($"Unmute: user {name} not found", "Server"));
            return;
        }

        if (target?.Muted() == false || (id != null && !Client.MutedList.ContainsKey((int)id))) {
            client.Send(Utils.BuildServerSideMessage($"Unmute: {name} is not muted", "Server"));
        } else {
            if (target != null) {
                target.RealMuted = false;
                target.TempMuted = false; // Undo this too I guess.
            }
            client.Send(Utils.BuildServerSideMessage($"Unmute: {name} has been unmuted", "Server"));
            if (id != null) {
                Client.MutedList.Remove((int)id);
                Client.SaveMutedBanned();
            }
        }
    }
}