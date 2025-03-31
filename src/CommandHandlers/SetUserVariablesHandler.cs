using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("SUV")]
class SetUserVariablesHandler : CommandHandler {
    NetworkObject suvData;
    Client client;
    string? uid;
    public override Task Handle(Client client, NetworkObject receivedObject) {
        if (client.Room == null) {
            Console.WriteLine($"SUV Missing Room IID: {client.ClientID}");
            client.Send(NetworkObject.WrapObject(0, 1006, new NetworkObject()).Serialize());
            client.ScheduleDisconnect();
            return Task.CompletedTask;
        }
        this.client = client;
        suvData = receivedObject.Get<NetworkObject>("p");
        uid = suvData.Get<string>("UID");

        // TODO
        if (uid != null && (client.PlayerData.Uid != uid || !client.PlayerData.IsValid)) {
            Console.WriteLine($"SUV {client.Room.Name} ({client.Room.ClientsCount}) IID: {client.ClientID} UID: {uid}");
            client.PlayerData.Uid = uid;
            client.PlayerData.InitFromNetworkData(suvData);
            UpdatePlayersInRoom();
            SendSUVToPlayerInRoom();
            if (client.Room is SpecialRoom room) room.SendAllAlerts(client);
        } else {
            UpdateVars();
        }
        
        return Task.CompletedTask;
    }

    private void UpdateVars() {
        bool updated = false;
        NetworkArray vl = new();
        NetworkObject data = new();

        foreach (string varName in PlayerData.SupportedVariables) {
            string? value = suvData.Get<string>(varName);
            if (value != null) {
                value = client.PlayerData.SetVariable(varName, value);
                updated = true;
                data.Add(varName, value);
                vl.Add(NetworkArray.Param(varName, value));
            }
        }

        if (updated) {
            client.SendSUV(vl, data);
        }
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

    private void SendSUVToPlayerInRoom() {
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
}
