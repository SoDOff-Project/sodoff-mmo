using sodoffmmo.Data;

namespace sodoffmmo.Core;

public class AmbassadorRoom : Room {
    public double[] ambassadorGauges;
    readonly System.Timers.Timer? ambassadorTimer;

    public AmbassadorRoom(string name) : base(name) {
        Console.WriteLine($"Setup Ambassador for {name}");
        ambassadorGauges = new double[3]; // There is always a maximum of 3.
        for (int i=0;i<3;i++) ambassadorGauges[i] = Configuration.ServerConfiguration.AmbassadorGaugeStart;
        ambassadorTimer = new(Configuration.ServerConfiguration.AmbassadorGaugeDecayRate * 1000) {
            AutoReset = true,
            Enabled = true
        };
        ambassadorTimer.Elapsed += (sender, e) => {
            if (!Configuration.ServerConfiguration.AmbassadorGaugeDecayOnlyWhenInRoom || clients.Count > 0) {
                for (int i=0;i<3;i++) ambassadorGauges[i] = Math.Max(0, ambassadorGauges[i]-1);
                NetworkArray vars = new();
                AddRoomData(vars);
                Send(Utils.VlNetworkPacket(vars, Id));
            }
        };
    }

    internal override void AddRoomData(NetworkArray vars) {
        base.AddRoomData(vars);
        vars.Add(NetworkArray.VlElement("COUNT",  (int)Math.Round(ambassadorGauges[0]), isPersistent: true));
        vars.Add(NetworkArray.VlElement("COUNT2", (int)Math.Round(ambassadorGauges[1]), isPersistent: true));
        vars.Add(NetworkArray.VlElement("COUNT3", (int)Math.Round(ambassadorGauges[2]), isPersistent: true));
    }
}
