using sodoffmmo.Core;
using sodoffmmo.Data;


namespace sodoffmmo.Module;
internal class SimulatedPlayer {
    public static Dictionary<string, SimulatedPlayer> SimulatedPlayers { get; } = new();

    DummyClient client = new();
    string room;
    System.Timers.Timer spvTimer = new(2500);
    System.Timers.Timer collisionTimer = new(500);
    HashSet<Client> prevCollision = new();
    HashSet<Client> currCollision = new();
    HashSet<string> rewardedPlayers = new();

    public SimulatedPlayer(Client creator) {
        client.PlayerData.DisplayName = "";
        room = creator.Room.Name;
        string id = $"3{Random.Shared.Next(0, 1_000_000):D6}";
        string avatarParts = "{\"PartType\":\"Version\",\"Offsets\":[{\"X\":6.0,\"Y\":1.0,\"Z\":0.0}],\"Geometries\":null,\"Textures\":null}";
        avatarParts += ",{\"PartType\":\"Eyes\",\"Offsets\":null,\"Geometries\":[\"__EMPTY__\"],\"Textures\":[null],\"Attributes\":[]}";
        avatarParts += ",{\"PartType\":\"Mouth\",\"Offsets\":null,\"Geometries\":[\"__EMPTY__\"],\"Textures\":[null,null],\"Attributes\":null}";
        avatarParts += ",{\"PartType\":\"Skin\",\"Offsets\":null,\"Geometries\":[\"__EMPTY__\"],\"Textures\":[null],\"Attributes\":null}";
        avatarParts += ",{\"PartType\":\"Hand\",\"Offsets\":null,\"Geometries\":[\"NULL\",\"NULL\"],\"Textures\":[null,null],\"Attributes\":[{\"Key\":\"CustomSuit\",\"Value\":\"True\"},{\"Key\":\"DisableBlink\",\"Value\":\"true\"}]}";
        avatarParts += ",{\"PartType\":\"Feet\",\"Offsets\":null,\"Geometries\":[\"NULL\",\"NULL\"],\"Textures\":[null,null],\"Attributes\":[]}";
        avatarParts += ",{\"PartType\":\"Hair\",\"Offsets\":null,\"Geometries\":[\"NULL\"],\"Textures\":[null,null,null],\"Attributes\":[]}";
        avatarParts += ",{\"PartType\":\"WristBand\",\"Offsets\":null,\"Geometries\":[\"NULL\"],\"Textures\":[null,null],\"Attributes\":[]}";
        avatarParts += ",{\"PartType\":\"Head\",\"Offsets\":null,\"Geometries\":[\"NULL\"],\"Textures\":[\"DWAvatarFacialDecals01.unity3d/Decal_Blank\",\"DWAvatarFacialDecals01.unity3d/Decal_Blank\",\"DWAvatarFacialDecals01.unity3d/Decal_Blank\",\"DWAvatarFacialDecals01.unity3d/Decal_Blank\",\"DWAvatarFacialDecals01.unity3d/Decal_Blank\",\"DWAvatarFacialWarPaints01.unity3d/Decal_Fill\"],\"Attributes\":[]}";
        avatarParts += ",{\"PartType\":\"Legs\",\"Offsets\":null,\"Geometries\":[\"NULL\"],\"Textures\":[null],\"Attributes\":[]}";
        avatarParts += ",{\"PartType\":\"Torso\",\"Offsets\":null,\"Geometries\":[\"NULL\"],\"Textures\":[null],\"Attributes\":null}";

        client.PlayerData.Uid = $"0{id}67-89ab-cdef-fedc-ba9876543211";
        client.PlayerData.ZoneName = creator.PlayerData.ZoneName;
        client.PlayerData.SetVariable("L", creator.Room.Name);
        client.PlayerData.SetVariable("A", $"{{\"Id\":{client.ClientID},\"DisplayName\":\" \",\"Part\":[{avatarParts}],\"GenderType\":1}}");
        client.PlayerData.SetVariable("FP", creator.PlayerData.GetVariable("FP"));
        client.PlayerData.SetVariable("MBF", "8");
        client.PlayerData.SetVariable("F", "9");
        client.PlayerData.SetVariable("J", "2");
        client.PlayerData.SetVariable("BU", "False");
        client.PlayerData.SetVariable("UDT", "");
        client.PlayerData.SetVariable("RA", "{\"P\":5,\"R\":2}");
        client.PlayerData.SetVariable("UDT", "");
        client.PlayerData.SetVariable("CU", "-1");
        client.PlayerData.SetVariable("M", "True");
        client.PlayerData.SetVariable("PU", "");
        client.PlayerData.P1 = creator.PlayerData.P1;
        client.PlayerData.P2 = creator.PlayerData.P2;
        client.PlayerData.P3 = creator.PlayerData.P3;
        client.PlayerData.R = creator.PlayerData.R;
        client.PlayerData.R1 = creator.PlayerData.R1;
        client.PlayerData.R2 = creator.PlayerData.R2;
        client.PlayerData.R3 = creator.PlayerData.R3;
        client.PlayerData.IsValid = true;
    }

    public async Task InitSimulatedPlayer() {
        client.SetRoom(Room.Get(room));
        client.PlayerData.IsValid = true;
        await Task.Delay(1000);
        UpdatePlayersInRoom();
        SendSUVToPlayersInRoom();
        await Task.Delay(1000);
        SendSPV();

        spvTimer.AutoReset = true;
        spvTimer.Elapsed += (s, e) => SendSPV();
        spvTimer.Start();
        collisionTimer.AutoReset = true;
        collisionTimer.Elapsed += (s, e) => CollisionCheck();
        collisionTimer.Start();
    }

    public void Destroy() {
        spvTimer.Stop();
        spvTimer.Dispose();
        collisionTimer.Stop();
        collisionTimer.Dispose();
        client.SetRoom(null);
    }

    private void UpdatePlayersInRoom() {
        NetworkObject data = new();
        NetworkObject acknowledgement = new();
        data.Add("r", client.Room.Id);

        NetworkArray user = client.PlayerData.GetNetworkData(client.ClientID, out NetworkArray playerData);
        data.Add("u", user);

        acknowledgement.Add("u", client.ClientID);
        acknowledgement.Add("vl", playerData);
        NetworkPacket ackPacket = NetworkObject.WrapObject(0, 12, acknowledgement).Serialize();
        NetworkObject obj = ackPacket.GetObject();
        ackPacket.Compress();
        client.Send(ackPacket);

        NetworkPacket packet = NetworkObject.WrapObject(0, 1000, data).Serialize();
        packet.Compress();

        client.Room.Send(packet, client);
    }

    private void SendSUVToPlayersInRoom() {
        NetworkObject cmd = new();
        NetworkObject obj = new();

        cmd.Add("c", "SUV");
        if (client.OldApi) {
            obj.Add("MID", client.ClientID.ToString());
        } else {
            obj.Add("MID", client.ClientID);
        }
        cmd.Add("p", obj);

        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Room.Send(packet, client);
    }

    private void SendSPV() {
        NetworkObject cmd = new();
        NetworkObject obj = new();
        NetworkArray container = new();
        NetworkObject vars = new();
        vars.Add("R", client.PlayerData.R);
        vars.Add("U", new float[] { client.PlayerData.P1, client.PlayerData.P2, client.PlayerData.P3, client.PlayerData.R1, client.PlayerData.R2, client.PlayerData.R3 });
        vars.Add("MX", 21);
        vars.Add("F", 2);
        vars.Add("MBF", client.PlayerData.Mbf);
        vars.Add("NT", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        vars.Add("t", (int)(Runtime.CurrentRuntime / 1000));
        vars.Add("MID", client.ClientID);

        container.Add(vars);
        obj.Add("arr", container);
        cmd.Add("c", "SPV");
        cmd.Add("p", obj);

        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Room.Send(packet, client);
    }

    void CollisionCheck() {
        currCollision.Clear();
        double radius = 3.0 * 3.0;

        foreach (var c in client.Room.Clients) {
            if (c == client)
                continue;

            var p = c.PlayerData;
            double dx = p.P1 - client.PlayerData.P1;
            double dy = p.P2 - client.PlayerData.P2;
            double dz = p.P3 - client.PlayerData.P3;
            double dist = dx * dx + dy * dy + dz * dz;

            if (dist <= radius) {
                currCollision.Add(c);
                if (prevCollision.Contains(c))
                    continue;
                if (rewardedPlayers.Contains(c.PlayerData.Uid)) {
                    c.Send(Utils.BuildServerSideMessage("I can't give you any points, you've already found me!", "Hidden Dragon"));
                    continue;
                }
                c.Send(Utils.BuildServerSideMessage("Congratulations! You found me!", "Hidden Dragon"));
                rewardedPlayers.Add(c.PlayerData.Uid);
                // TODO: call clan points endpoint
            }
        }
        (prevCollision, currCollision) = (currCollision, prevCollision);
    }
}
