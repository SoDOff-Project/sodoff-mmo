using System.Text.RegularExpressions;
using sodoffmmo.Core;

namespace sodoffmmo.Data;
public class PlayerData {
    // rotation (eulerAngles.y)
    public double R { get; set; }
    // velocity x
    public double R1 { get; set; }
    // velocity y
    public double R2 { get; set; }
    // velocity z
    public double R3 { get; set; }
    // max speed
    public double Mx { get; set; } = 6;
    // UDT points
    public string Udt { get; set; } = "";
    // position x
    public double P1 { get; set; }
    // position y
    public double P2 { get; set; }
    // position z
    public double P3 { get; set; }

    
    // join (?)
    public string J { get; set; } = "2";
    // flags (?)
    public int F { get; set; }
    // animation bitfield (animations used by avatar, e.g. mounted, swim, ...)
    public int Mbf { get; set; }
    // busy (?)
    public string Bu { get; set; } = "False";
    // viking uid
    public string Uid { get; set; } = "";
    // (not raised) pet data
    public string Pu { get; set; } = "";
    // avatar data
    public string A { get; set; } = "";
    // XP rank (points and level)
    public string Ra { get; set; } = "";
    // country info (for flag?)
    public string Cu { get; set; } = "-1";
    // membership status
    public string M { get; set; } = "False";
    // location (level)
    public string L { get; set; } = "";
    // client token
    public string UNToken { get; set; } = "";
    // raised pet geometry - set from Fp
    public PetGeometryType GeometryType { get; set; } = PetGeometryType.Default;
    // raised pet age - set from Fp
    public PetAge PetAge { get; set; } = PetAge.Adult;
    // raised pet mounted - set from Fp
    public bool PetMounted { get; set; } = false;
    // raised pet data
    public string Fp {
        get {
            return fp;
        }
        set {
            string[] array = value.Split('*');
            Dictionary<string, string> keyValPairs = new();
            foreach (string str in array) {
                string[] keyValPair = str.Split('$');
                if (keyValPair.Length == 2)
                    keyValPairs[keyValPair[0]] = keyValPair[1];
            }
            GeometryType = PetGeometryType.Default;
            PetAge = PetAge.Adult;
            if (keyValPairs.TryGetValue("G", out string geometry))
                if (geometry.ToLower().Contains("terribleterror"))
                    GeometryType = PetGeometryType.Terror;
            if (keyValPairs.TryGetValue("A", out string age)) {
                switch (age) {
                    case "E": PetAge = PetAge.EggInHand; break;
                    case "B": PetAge = PetAge.Baby;      break;
                    case "C": PetAge = PetAge.Child;     break;
                    case "T": PetAge = PetAge.Teen;      break;
                    case "A": PetAge = PetAge.Adult;     break;
                    case "Ti": PetAge = PetAge.Titan;    break;
                }
            }
            if (keyValPairs.TryGetValue("U", out string userdata)) {
                PetMounted = (userdata == "0" || userdata == "1");
            }
            if (PetMounted &&
                (GeometryType == PetGeometryType.Default && PetAge < PetAge.Teen
                || GeometryType == PetGeometryType.Terror && PetAge < PetAge.Titan)
            ) {
                fp = Regex.Replace(value, "^U\\$[01]\\*", "U$-1*");
            } else {
                fp = value;
            }
        }
    }
    private string fp = "";

    public NetworkArray GetNetworkData(int clientID, out NetworkArray paramArr) {
        NetworkArray arr = new();
        arr.Add(clientID);
        arr.Add(Uid);
        arr.Add((short)1);
        arr.Add((short)clientID);

        paramArr = new();
        paramArr.Add(NetworkArray.Param("R1", R1));
        paramArr.Add(NetworkArray.Param("FP", Fp));
        paramArr.Add(NetworkArray.Param("MX", Mx));
        paramArr.Add(NetworkArray.Param("UDT", Udt));
        paramArr.Add(NetworkArray.Param("P2", P2));
        paramArr.Add(NetworkArray.Param("NT", (double)(Runtime.CurrentRuntime))); // network time
        paramArr.Add(NetworkArray.Param("t", (int)(Runtime.CurrentRuntime / 1000))); // timestamp
        paramArr.Add(NetworkArray.Param("J", J));
        paramArr.Add(NetworkArray.Param("F", F));
        paramArr.Add(NetworkArray.Param("MBF", Mbf));
        paramArr.Add(NetworkArray.Param("R2", R2));
        paramArr.Add(NetworkArray.Param("R", R));
        paramArr.Add(NetworkArray.Param("BU", Bu));
        paramArr.Add(NetworkArray.Param("P1", P1));
        paramArr.Add(NetworkArray.Param("UID", Uid));
        paramArr.Add(NetworkArray.Param("R3", R3));
        paramArr.Add(NetworkArray.Param("PU", Pu));
        paramArr.Add(NetworkArray.Param("A", A));
        paramArr.Add(NetworkArray.Param("RA", Ra));
        paramArr.Add(NetworkArray.Param("P3", P3));
        paramArr.Add(NetworkArray.Param("CU", Cu));
        paramArr.Add(NetworkArray.Param("M", M));
        paramArr.Add(NetworkArray.Param("L", L));

        arr.Add(paramArr);
        return arr;
    }
}

public enum PetGeometryType {
    Default,
    Terror
}
public enum PetAge {
    EggInHand = 0,
    Baby = 1,
    Child = 2,
    Teen = 3,
    Adult = 4,
    Titan = 5
}
