using sodoffmmo.Data;
using System;

namespace sodoffmmo.Core;
public class DragonTagRoom : Room {
    Client? it;
    bool tagInProgress = false;
    System.Timers.Timer collisionTimer;
    DateTime lastTagTime = DateTime.MinValue;
    DateTime lastEmoteTime = DateTime.MinValue;
    readonly object _lock = new object();

    public DragonTagRoom(string name) : base(name) {
        collisionTimer = new(125);
        collisionTimer.AutoReset = true;
        collisionTimer.Elapsed += (s, e) => CollisionCheck();
    }

    public override void AddClient(Client client) {
        base.AddClient(client);
        lock (_lock) {
            if (tagInProgress)
                return;
            if (ClientsCount > 1)
                StartTagGame();
        }
    }

    public override void RemoveClient(Client client) {
        base.RemoveClient(client);
        lock (_lock) {
            if (!tagInProgress)
                return;
            if (ClientsCount < 2)
                StopTagGame();
            else if (client == it)
                ChooseNewIt();
        }
    }

    void StartTagGame() {
        ChooseNewIt();
        tagInProgress = true;
        lastTagTime = DateTime.UtcNow;
        collisionTimer.Start();
    }
    void StopTagGame() {
        it = null;
        tagInProgress = false;
        collisionTimer.Stop();
        Send(Utils.BuildServerSideMessage("Dragon Tag game has ended!", "Dragon Tag"));
    }

    void ChooseNewIt() {
        it = Clients.ToList()[Random.Shared.Next(ClientsCount)];
        Send(Utils.BuildServerSideMessage($"{it.PlayerData.DisplayName} is it. Run!", "Dragon Tag"));
    }

    void CollisionCheck() {
        if (DateTime.UtcNow - lastEmoteTime > TimeSpan.FromSeconds(3)) {
            Send(CreateEmoteNetObject(13).Serialize());
            lastEmoteTime = DateTime.UtcNow;
        }

        lock (_lock) {
            if (!tagInProgress || it == null)
                return;

            if (DateTime.UtcNow - lastTagTime < TimeSpan.FromSeconds(5))
                return;

            double radius = 3.0 * 3.0;

            foreach (var client in Clients) {
                if (client == it)
                    continue;

                var p = client.PlayerData;
                double dx = p.P1 - it.PlayerData.P1;
                double dy = p.P2 - it.PlayerData.P2;
                double dz = p.P3 - it.PlayerData.P3;
                double dist = dx * dx + dy * dy + dz * dz;

                if (dist <= radius) {
                    PassTag(client);
                    return;
                }
            }
        }
    }

    void PassTag(Client newIt) {
        Send(CreateEmoteNetObject(3).Serialize());
        it = newIt;
        lastTagTime = DateTime.UtcNow;
        Send(Utils.BuildServerSideMessage($"{it.PlayerData.DisplayName} is it. Run!", "Dragon Tag"));
    }

    NetworkObject CreateEmoteNetObject(int id) {
        NetworkObject obj = new();
        obj.Add("t", (byte)0);
        obj.Add("r", Id);
        obj.Add("u", it!.ClientID);
        obj.Add("m", $"EID:{id}");
        return NetworkObject.WrapObject(0, 7, obj);
    }
}
