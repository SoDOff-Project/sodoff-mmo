using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.CommandHandlers;

[ExtensionCommandHandler("SPV")]
class SetPositionVariablesHandler : CommandHandler {
    Client client;
    NetworkObject spvData;
    public override Task Handle(Client client, NetworkObject receivedObject) {
        if (client.Room == null) {
            Console.WriteLine($"SPV Missing Room IID: {client.ClientID}");
            client.Send(NetworkObject.WrapObject(0, 1006, new NetworkObject()).Serialize());
            client.ScheduleDisconnect();
            return Task.CompletedTask;
        }
        this.client = client;
        spvData = receivedObject.Get<NetworkObject>("p");
        UpdatePositionVariables();
        SendSPVCommand();
        
        return Task.CompletedTask;
    }

    private void UpdatePositionVariables() {
        float[] pos = spvData.Get<float[]>("U");
        client.PlayerData.R = spvData.Get<float>("R");
        client.PlayerData.P1 = pos[0];
        client.PlayerData.P2 = pos[1];
        client.PlayerData.P3 = pos[2];
        client.PlayerData.R1 = pos[3];
        client.PlayerData.R2 = pos[4];
        client.PlayerData.R3 = pos[5];
        client.PlayerData.Mx = spvData.Get<float>("MX");
        client.PlayerData.F = spvData.Get<int>("F");
        client.PlayerData.Mbf = spvData.Get<int>("MBF");
    }

    private void SendSPVCommand() {
        NetworkObject cmd = new();
        NetworkObject obj = new();
        NetworkArray container = new();
        NetworkObject vars = new();
        vars.Add("R", client.PlayerData.R);
        vars.Add("U", new float[] { client.PlayerData.P1, client.PlayerData.P2, client.PlayerData.P3, client.PlayerData.R1, client.PlayerData.R2, client.PlayerData.R3 });
        vars.Add("MX", client.PlayerData.Mx);
        vars.Add("F", client.PlayerData.F);
        vars.Add("MBF", client.PlayerData.Mbf);

        // user event
        string? ue = spvData.Get<string>("UE");
        if (ue != null)
            vars.Add("UE", ue);
        // pitch
        float? cup = spvData.Get<float>("CUP");
        if (cup != null)
            vars.Add("CUP", (float)cup);

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
}
