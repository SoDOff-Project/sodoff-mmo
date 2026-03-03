using sodoffmmo.Data;

namespace sodoffmmo.Core;

public class SpecialRoom : Room {
    public double[] ambassadorGauges = new double[3]; // There is always a maximum of 3.
    System.Timers.Timer? ambassadorTimer;

    public static void CreateRooms() {
        foreach (var room in Configuration.ServerConfiguration.RoomAlerts) {
            foreach (var alert in room.Value) {
                AlertInfo alertInfo = new AlertInfo(
                    alert[0], // type
                    float.Parse(alert[1], System.Globalization.CultureInfo.InvariantCulture.NumberFormat), // duration
                    Int32.Parse(alert[2]), Int32.Parse(alert[3]), // start min - max for random start time
                    Int32.Parse(alert[4]), Int32.Parse(alert[5])  // extra parameters for specific alarm types
                );
                Console.WriteLine($"Setup alert {alertInfo} for {room.Key}");
                (rooms.GetValueOrDefault(room.Key) as SpecialRoom ?? new SpecialRoom(room.Key)).AddAlert(alertInfo);
            }
        }

        foreach (var room in Configuration.ServerConfiguration.AmbassadorRooms) {
            Console.WriteLine($"Setup Ambassador for {room}");
            (rooms.GetValueOrDefault(room) as SpecialRoom ?? new SpecialRoom(room)).InitAmbassador();
        }
    }

    public SpecialRoom(string name) : base(name) {}

    public void InitAmbassador() {
        for (int i=0;i<3;i++) ambassadorGauges[i] = Configuration.ServerConfiguration.AmbassadorGaugeStart;
        ambassadorTimer = new(Configuration.ServerConfiguration.AmbassadorGaugeDecayRate * 1000) {
            AutoReset = true,
            Enabled = true
        };
        ambassadorTimer.Elapsed += (sender, e) => {
            if (!Configuration.ServerConfiguration.AmbassadorGaugeDecayOnlyWhenInRoom || ClientsCount > 0) {
                for (int i=0;i<3;i++) ambassadorGauges[i] = Math.Max(0, ambassadorGauges[i]-1);
                Send(Utils.VlNetworkPacket(GetRoomVars(), Id));
            }
        };
    }

    internal override NetworkArray GetRoomVars() {
        NetworkArray vars = new();
        vars.Add(NetworkArray.VlElement("COUNT",  (int)Math.Round(ambassadorGauges[0]), isPersistent: true));
        vars.Add(NetworkArray.VlElement("COUNT2", (int)Math.Round(ambassadorGauges[1]), isPersistent: true));
        vars.Add(NetworkArray.VlElement("COUNT3", (int)Math.Round(ambassadorGauges[2]), isPersistent: true));
        for (int i=0;i<RoomVariables.Length;i++) vars.Add(RoomVariables[i]);
        return vars;
    }

    private int alertId = -1;
    private Random random = new();

    List<AlertInfo> alerts = new();

    public void AddAlert(AlertInfo alert) {
        alerts.Add(alert);
        ResetAlertTimer(alert);
    }

    public void SendAllAlerts(Client client) {
        return; // Disables joining ongoing alerts (since it doesn't work properly).
        
        foreach (AlertInfo alert in alerts) {
            if (alert.IsRunning()) StartAlert(alert, client);
        }
    }


    private void StartAlert(AlertInfo alert, Client? specificClient = null) {
        NetworkArray NewRoomVariables = new();
        NewRoomVariables.Add(NetworkArray.VlElement(REDALERT_START, alertId++, isPersistent: true));
        NewRoomVariables.Add(NetworkArray.VlElement(REDALERT_TYPE, alert.type, isPersistent: true));
        double duration = (alert.endTime - DateTime.Now).TotalSeconds;
        NewRoomVariables.Add(NetworkArray.VlElement(REDALERT_LENGTH, alert.type == "1" ? alert.redAlertDuration : duration, isPersistent: true));
        if (alert.type == "1") {
            NewRoomVariables.Add(NetworkArray.VlElement(REDALERT_TIMEOUT, duration, isPersistent: true));
        } else if (alert.type == "3") {
            alert.songId = random.Next(0, alert.songs);
            NewRoomVariables.Add(NetworkArray.VlElement(REDALERT_SONG, (double)alert.songId, isPersistent: true));
        }
        NetworkPacket packet = Utils.VlNetworkPacket(NewRoomVariables, Id);
        if (specificClient is null) {
            RoomVariables = NewRoomVariables;
            Send(packet);
            RoomVariables = new();
            Console.WriteLine("Started event " +alert + " in room " + Name);
        } else {
            specificClient.Send(packet);
            Console.WriteLine("Added " + specificClient.PlayerData.DisplayName + " to event " + alert + " with " + duration + " seconds remaining");
        }
    }

    void ResetAlertTimer(AlertInfo alert) {
        System.Timers.Timer? timer = alert.timer;
        if (timer != null) {
            timer.Stop();
            timer.Close();
        }
        DateTime startTime = DateTime.Now.AddMilliseconds(random.Next(alert.minTime * 1000, alert.maxTime * 1000));
        DateTime endTime = startTime.AddSeconds(alert.duration);
        for (int i = 0; i < alerts.IndexOf(alert); i++) {
            // Prevent overlap between two events.
            if (alerts[i].Overlaps(endTime)) {
                startTime = alerts[i].endTime.AddSeconds(5);
                endTime = startTime.AddSeconds(alert.duration);
            }
        }
        timer = new System.Timers.Timer((startTime - DateTime.Now).TotalMilliseconds);
        timer.AutoReset = false;
        timer.Enabled = true;
        timer.Elapsed += (sender, e) => StartAlert(alert);
        timer.Elapsed += (sender, e) => ResetAlertTimer(alert);
        alert.timer = timer;
        Console.WriteLine("Event " + alert + " in " + Name + " scheduled for " + startTime.ToString("MM/dd/yyyy HH:mm:ss tt") + " (in " + (startTime - DateTime.Now).TotalSeconds + " seconds)");
        alert.startTime = startTime;
        alert.endTime = endTime;
    }

    private const string REDALERT_START = "RA_S";
    private const string REDALERT_TYPE = "RA_A";
    private const string REDALERT_LENGTH = "RA_L";
    private const string REDALERT_TIMEOUT = "RA_T";
    private const string REDALERT_SONG = "RA_SO";

    public class AlertInfo {
        public readonly string type;
        public readonly double duration;
        public readonly int minTime;
        public readonly int maxTime;
        public readonly int redAlertDuration;
        public readonly int songs;
        public int songId;

        public DateTime startTime {
            get {
                return newStartTime;
            }
            set {
                oldStartTime = newStartTime;
                newStartTime = value;
            }
        }

        public DateTime endTime {
            get {
                return newEndTime;
            }
            set {
                oldEndTime = newEndTime;
                newEndTime = value;
            }
        }

        private DateTime newStartTime;
        private DateTime newEndTime;
        private DateTime oldStartTime;
        private DateTime oldEndTime;

        public System.Timers.Timer? timer = null;

        public AlertInfo(string type, double duration = 20.0, int minTime = 30, int maxTime = 240, int redAlertDuration = 60, int songs = 16) {
            this.type = type;
            this.duration = duration;
            this.minTime = minTime;
            this.maxTime = maxTime;
            this.redAlertDuration = redAlertDuration;
            this.songs = songs;
        }

        public bool Overlaps(DateTime time) {
            return (time >= oldStartTime && time <= oldEndTime);
        }

        public bool IsRunning() {
            return Overlaps(DateTime.Now);
        }

        public override string ToString() {
            return type switch {
                "1" => "RedAlert",
                "2" => "DiscoAlert",
                "3" => "DanceOff",
                _ => type
            };
        }
    }
}
