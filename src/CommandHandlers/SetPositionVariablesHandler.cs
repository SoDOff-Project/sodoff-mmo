using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("SPV")]
class SetPositionVariablesHandler : ICommandHandler {
    Client client;
    NetworkObject spvData;
    public void Handle(Client client, NetworkObject receivedObject) {
        if (client.Room == null) {
            Console.WriteLine($"SPV Missing Room IID: {client.ClientID}");
            client.Send(NetworkObject.WrapObject(0, 1006, new NetworkObject()).Serialize());
            client.ScheduleDisconnect();
            return;
        }
        this.client = client;
        spvData = receivedObject;
        UpdatePositionVariables();
        SendSPVCommand();
    }

    private void UpdatePositionVariables() {
        NetworkObject obj = spvData.Get<NetworkObject>("p");
        float[] pos = obj.Get<float[]>("U");
        client.PlayerData.R = obj.Get<float>("R");
        client.PlayerData.P1 = pos[0];
        client.PlayerData.P2 = pos[1];
        client.PlayerData.P3 = pos[2];
        client.PlayerData.R1 = pos[3];
        client.PlayerData.R2 = pos[4];
        client.PlayerData.R3 = pos[5];
        client.PlayerData.Mx = obj.Get<float>("MX");
        client.PlayerData.F = obj.Get<int>("F");
        client.PlayerData.Mbf = obj.Get<int>("MBF");
    }

    private void SendSPVCommand() {
        NetworkObject cmd = new();
        NetworkObject obj = new();
        NetworkArray container = new();
        NetworkObject vars = new();
        vars.Add("MX", (float)client.PlayerData.Mx);
        vars.Add("ST", Runtime.CurrentRuntime);
        vars.Add("NT", Runtime.CurrentRuntime.ToString());
        vars.Add("t", (int)(Runtime.CurrentRuntime / 1000));
        vars.Add("F", client.PlayerData.F);
        vars.Add("MBF", client.PlayerData.Mbf);
        vars.Add("R", client.PlayerData.R);
        vars.Add("U", new float[] { (float)client.PlayerData.P1, (float)client.PlayerData.P2, (float)client.PlayerData.P3, (float)client.PlayerData.R1, (float)client.PlayerData.R2, (float)client.PlayerData.R3 });
        vars.Add("MID", client.ClientID);
        string? ue = spvData.Get<NetworkObject>("p").Get<string>("UE");
        if (ue != null)
            vars.Add("UE", ue);

        container.Add(vars);
        obj.Add("arr", container);
        cmd.Add("c", "SPV");
        cmd.Add("p", obj);

        NetworkPacket packet = NetworkObject.WrapObject(1, 13, cmd).Serialize();
        client.Room.Send(packet, client);
    }
}
