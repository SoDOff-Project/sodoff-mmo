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
            client.Room.SendAllAlerts(client);
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
                client.PlayerData.SetVariable(varName, value);
                updated = true;
                data.Add(varName, value);
                vl.Add(NetworkArray.Param(varName, value));
            }
        }

        if (!updated) {
            return;
        }

        NetworkObject data2 = new();
        data2.Add("u", client.ClientID);
        data2.Add("vl", vl);
        NetworkPacket packet = NetworkObject.WrapObject(0, 12, data2).Serialize();
        client.Room.Send(packet);

        NetworkObject cmd = new();
        cmd.Add("c", "SUV");
        NetworkArray arr = new();
        if (client.OldApi) {
            data.Add("MID", client.ClientID.ToString());
        } else {
            data.Add("MID", client.ClientID);
        }
        data.Add("RID", client.Room.Id.ToString());
        arr.Add(data);
        NetworkObject container = new();
        container.Add("arr", arr);
        cmd.Add("p", container);
        packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Room.Send(packet, client);
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
