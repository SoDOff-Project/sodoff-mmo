using System.Globalization;
using sodoffmmo.Data;
using System.Timers;

namespace sodoffmmo.Core;

public enum RacingPlayerState {
    NotReady,
    Ready,
    InRacingRoom,
    RaceReady1,
    RaceReady2
}

public class RacingRoom : Room {
    static Random random = new Random();

    public static RacingRoom Get() {
        return new RacingRoom();
    }

    public RacingRoom() : base (null, "RacingDragon", true) {
        TID = random.Next(105);

        players = new();
        results = new();
        timer = null;

        base.RoomVariables = new();
        base.RoomVariables.Add(NetworkArray.VlElement("IS_RACE_ROOM", "SINGLERACE#1#1#SINGLERACE#0#2"));
        base.RoomVariables.Add(NetworkArray.VlElement("TID", TID));
    }
    
    public int TID;

    // players ready status

    Dictionary<Client, RacingPlayerState> players;
    
    public void SetPlayerState(Client client, RacingPlayerState state) {
        players[client] = state;
    }

    public bool IsPlayerState(Client client, RacingPlayerState state) {
        if (players.TryGetValue(client, out var info)) {
            return info == state;
        }
        return false;
    }

    public int GetPlayersCount(RacingPlayerState state) {
        int count = 0;
        foreach(var player in players) {
            if (player.Value == state) ++count;
        }
        return count;
    }

    // results

    class Result {
        public string userName;
        public string time;
        public string laps;

        public Result(string userName, string time, string laps) {
            this.userName = userName;
            this.time = time;
            this.laps = laps;
        }
    }

    SortedDictionary<float, Result> results;

    public void SetResults(Client client, string userName, string time, string laps) {
        float timef = float.Parse(time, System.Globalization.CultureInfo.InvariantCulture);
        while (results.ContainsKey(timef))
            timef += 0.000001f;

        results.Add(timef, new Result(userName, time, laps));
    }

    public void SendResults() {
        if (ClientsCount == results.Count) {
            // {"a":13,"c":1,"p":{"c":"","p":{"arr":["RA","","GR","Zavertin","91.81613","3","scourgexxwulf","111.81613","3"]},"r":412467}}
            List<string> info = new();
            info.Add("RA");
            info.Add("");
            info.Add("GR");
            foreach(var result in results) {
                info.Add(result.Value.userName);
                info.Add(result.Value.time);
                info.Add(result.Value.laps);
            }

            NetworkPacket packet = Utils.ArrNetworkPacket(info.ToArray(), "", Id);
            Send(packet);
        }
    }

    // start and countdown

    System.Timers.Timer? timer = null;
    int counter;

    private void SetTimer(double timeout, System.Timers.ElapsedEventHandler callback, bool AutoReset = false) {
        if (timer != null) {
            timer.Stop();
            timer.Close();
        }
        
        timer = new System.Timers.Timer(timeout * 1000);
        timer.AutoReset = AutoReset;
        timer.Enabled = true;
        timer.Elapsed += callback;
    }

    public void Init() {
        counter = 20;
        SetTimer(0.2, SendJoin);
    }

    private void SendJoin(Object? source, ElapsedEventArgs e) {
        foreach(var player in players) {
            player.Key.SetRoom(this);
        }
        SetTimer(1, CountDown, true);
    }

    private void CountDown(Object? source, ElapsedEventArgs e) {
        if (!TryLoad()) {
            if (counter == 0) {
                Load();
            } else {
                // {"a":13,"c":1,"p":{"c":"","p":{"arr":["RA","","LT","18"]},"r":412467}}
                NetworkPacket packet = Utils.ArrNetworkPacket(new string[] {
                    "RA",
                    "",
                    "LT",
                    (--counter).ToString()
                }, "", Id);
                Send(packet);
            }
        }
    }
    
    public bool TryLoad() {
        if (GetPlayersCount(RacingPlayerState.Ready) == ClientsCount) {
            Load();
            return true;
        }
        return false;
    }

    public void Load() {
        timer!.Stop();
        timer!.Close();

        // {"a":13,"c":1,"p":{"c":"","p":{"arr":["RA","","ST"]},"r":412467}}
        NetworkPacket packet = Utils.ArrNetworkPacket(new string[] {
            "RA",
            "",
            "ST"
        }, "", Id);
        Send(packet);
    }

    // TODO StratTimer → kick out to main lobby players without RacingPlayerState.RaceReady1 after timeout, next kick out players without RacingPlayerState.RaceReady2 after timeout2

    // TODO EndTimer → {"a":13,"c":1,"p":{"c":"","p":{"arr":["RA","","ET","1"]},"r":412467}}

    // utils

    public NetworkPacket GetTIDPacket() {
        // {"a":13,"c":1,"p":{"c":"","p":{"arr":["RA","","TID","","10","0","0","0","0"]}}}
        return Utils.ArrNetworkPacket(new string[] {
            "RA",
            "",
            "TID",
            "", // game mode (unused)
            TID.ToString(), // track id
            "0", // theme id
            "0","0","0" // unused (?)
        });
    }

    public NetworkPacket GetSTAPacket() {
        string staList = "";
        int i = 0;
        foreach(var player in players) {
            if (i > 0)
                staList += ":";
            staList += player.Key.PlayerData.Uid + ":" + (++i).ToString();
        }

        // {"a":13,"c":1,"p":{"c":"","p":{"arr":["RA","","STA","c4647597-a72a-4f34-973c-5a10218d9a64:1:f05fc387-7358-4bff-be04-7c316f0a8de8:2:ae70ef16-c52c-43e4-9305-d6ea3e378a0d:3","1688128174649"]},"r":412467}}
        return Utils.ArrNetworkPacket(new string[] {
            "RA",
            "",
            "STA",
            staList
        }, "", Id);
    }
}
    
public class RacingLobby {
    static object lobbyLock = new object();
    public readonly static RacingLobby Lobby = new RacingLobby();

    private RacingLobby() {
        timer = new System.Timers.Timer(1000);
        timer.AutoReset = true;
        timer.Enabled = true;
        timer.Elapsed += CheckRacingRoomCountdown;
    }

    System.Timers.Timer timer;
    int counter;

    private void CheckRacingRoomCountdown(Object? source, System.Timers.ElapsedEventArgs e) {
        lock (lobbyLock) {
            int readyPlayersCount = GetPlayersCount(RacingPlayerState.Ready);
            if (readyPlayersCount >= Configuration.ServerConfiguration.RacingMaxPlayers) {
                SendToRacingRoom();
                counter = -1;
            } else if (readyPlayersCount >= Configuration.ServerConfiguration.RacingMinPlayers) {
                if (counter < 0) {
                    counter = Configuration.ServerConfiguration.RacingMainLobbyTimer;
                }
                if (--counter == 0) {
                    SendToRacingRoom();
                    counter = -1;
                }
            } else {
                counter = -1;
            }
        }
    }

    class Status {
        public string uid;
        public RacingPlayerState state = RacingPlayerState.NotReady;
        public Status (string uid) {
            this.uid = uid;
        }
    }

    Dictionary<Client, Status> lobbyPlayers = new();

    public void SetPlayerState(Client client, RacingPlayerState state) {
        lock (lobbyLock) {
            if (!lobbyPlayers.ContainsKey(client)) {
                lobbyPlayers[client] = new Status(client.PlayerData.Uid);
            }
            lobbyPlayers[client].state = state;
        }
    }

    public bool IsPlayerState(Client client, RacingPlayerState state) {
        lock (lobbyLock) {
            if (lobbyPlayers.TryGetValue(client, out var info)) {
                return info.state == state;
            }
            return false;
        }
    }

    public int GetPlayersCount(RacingPlayerState state) {
        lock (lobbyLock) {
            int count = 0;
            foreach(var player in lobbyPlayers) {
                if (player.Value.state == state) ++count;
            }
            return count;
        }
    }

    private bool SendToRacingRoom() {
        // lock (lobbyLock) {
            if (GetPlayersCount(RacingPlayerState.Ready) >= Configuration.ServerConfiguration.RacingMinPlayers) {
                int i = 0;
                RacingRoom room = RacingRoom.Get();
                foreach (var player in lobbyPlayers) {
                    if (player.Value.state == RacingPlayerState.Ready) {
                        if (++i > Configuration.ServerConfiguration.RacingMaxPlayers)
                            break;
                        // set client state in Lobby
                        player.Value.state = RacingPlayerState.InRacingRoom;
                        // send TID info to client
                        player.Key.Send(room.GetTIDPacket());
                        // set client state in racing room
                        room.SetPlayerState(player.Key, RacingPlayerState.NotReady);
                    }
                }
                // join clients to racing room and start countdown
                // after change room, client will be removed from lobbyPlayers (in GetPS)
                room.Init();

                return true;
            }
            return false;
        // }
    }
    
    public NetworkPacket GetPS() {
        List<Client> toRemove = new();
        Room room = Room.Get("DragonRacingDO");

        // {"a":13,"c":1,"p":{"c":"PS","p":{"arr":["RA","","PS","e6147216-8100-4552-864d-be8f1347e201","8cb5842d-735a-4259-80af-e2e204b9c2bd"]}}}
        List<string> info = new();
        info.Add("RA");
        info.Add("");
        info.Add("PS");
        foreach(var player in lobbyPlayers) {
            if (player.Key.Room != room) {
                toRemove.Add(player.Key);
            } else if (player.Value.state != RacingPlayerState.InRacingRoom) {
                info.Add(player.Value.uid);
            }
        }

        foreach (var player in toRemove) {
            lobbyPlayers.Remove(player);
        }

        return Utils.ArrNetworkPacket(info.ToArray(), "PS");
    }
}
