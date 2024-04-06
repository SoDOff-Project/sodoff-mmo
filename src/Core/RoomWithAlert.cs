using sodoffmmo.Data;
using System.Timers;

namespace sodoffmmo.Core;
class RoomWithAlert : Room {
    private int alertId;
    private System.Timers.Timer? timer = null;
    private Random random = new Random();
    
    private int minTime;
    private int maxTime;
    private int songs;
    
    public RoomWithAlert(string name, int minTime = 30, int maxTime = 240, int songs = 16) : base (name) {
        alertId = -1;
        
        this.minTime = minTime;
        this.maxTime = maxTime;
        this.songs = songs;
        
        SetTimer();
    }
    
    private void StartAlert(Object? source, ElapsedEventArgs e) {
        int songId = random.Next(0, songs);
        RoomVariables = new();
        RoomVariables.Add(NetworkArray.VlElement("RA_S", ++alertId, isPersistent: true));
        RoomVariables.Add(NetworkArray.VlElement("RA_A", "3", isPersistent: true));
        RoomVariables.Add(NetworkArray.VlElement("RA_L", 20.0, isPersistent: true));
        RoomVariables.Add(NetworkArray.VlElement("RA_SO", (double)songId, isPersistent: true));
        NetworkPacket packet = Utils.VlNetworkPacket(RoomVariables, Id);
        Send(packet);
        SetTimer();
    }
    
    private void SetTimer() {
        if (timer != null) {
            timer.Stop();
            timer.Close();
        }
        timer = new System.Timers.Timer(
            random.Next(minTime * 1000, maxTime * 1000)
        );
        timer.AutoReset = false;
        timer.Enabled = true;
        timer.Elapsed += StartAlert;
    }
}
