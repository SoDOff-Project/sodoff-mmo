using System.Text.RegularExpressions;
using sodoffmmo.Core;
using sodoffmmo.Management;

namespace sodoffmmo.Data;
public class PlayerData {
    public bool IsValid { get; set; } = false;

    // viking uid
    public string Uid { get; set; } = "";
    // client token
    public string UNToken { get; set; } = "";
    // avatar data
    public string A { get; set; } = "";
    // (not raised) pet data
    public string Pu { get; set; } = "";
    // raised pet data
    public string Fp {
        get {
            return fp;
        }
        set {
            fp = FixMountState(value);
        }
    }
    private string fp = "";

    // rotation (eulerAngles.y)
    public double R { get; set; }
    // velocity x
    public double R1 { get; set; }
    // velocity y
    public double R2 { get; set; }
    // velocity z
    public double R3 { get; set; }
    // position x
    public double P1 { get; set; }
    // position y
    public double P2 { get; set; }
    // position z
    public double P3 { get; set; }
    // animation bitfield (animations used by avatar, e.g. mounted, swim, ...)
    public int Mbf { get; set; }
    // max speed
    public double Mx { get; set; } = 6;
    // flags (?)
    public int F { get; set; }
    // location (level)
    public string L { get; set; } = "";

    // join allowed
    public string J { get; set; } = "2";
    // busy (?)
    public string Bu { get; set; } = "False";
    // UDT points
    public string Udt { get; set; } = "";
    // XP rank (points and level)
    public string Ra { get; set; } = "";
    // country info (for flag?)
    public string Cu { get; set; } = "-1";
    // membership status
    public string M { get; set; } = "True";

    public string DiplayName { get; set; } = "";
    public Role Role { get; set; } = Role.User;

    public NetworkArray GetNetworkData(int clientID, out NetworkArray paramArr) {
        NetworkArray arr = new();
        arr.Add(clientID);
        arr.Add(Uid);
        arr.Add((short)1);
        arr.Add((short)clientID);

        paramArr = new();
        paramArr.Add(NetworkArray.Param("NT", (double)(Runtime.CurrentRuntime))); // network time
        paramArr.Add(NetworkArray.Param("t", (int)(Runtime.CurrentRuntime / 1000))); // timestamp

        paramArr.Add(NetworkArray.Param("UID", Uid));
        paramArr.Add(NetworkArray.Param("A", A));
        paramArr.Add(NetworkArray.Param("PU", Pu));
        paramArr.Add(NetworkArray.Param("FP", Fp));

        if (IsValid) {
            paramArr.Add(NetworkArray.Param("R", R));
            paramArr.Add(NetworkArray.Param("R1", R1));
            paramArr.Add(NetworkArray.Param("R2", R2));
            paramArr.Add(NetworkArray.Param("R3", R3));
            paramArr.Add(NetworkArray.Param("P1", P1));
            paramArr.Add(NetworkArray.Param("P2", P2));
            paramArr.Add(NetworkArray.Param("P3", P3));
            paramArr.Add(NetworkArray.Param("MX", Mx));
            paramArr.Add(NetworkArray.Param("F", F));
            paramArr.Add(NetworkArray.Param("MBF", Mbf));
            paramArr.Add(NetworkArray.Param("L", L));

            paramArr.Add(NetworkArray.Param("J", J));
            paramArr.Add(NetworkArray.Param("BU", Bu));
            paramArr.Add(NetworkArray.Param("UDT", Udt));
            paramArr.Add(NetworkArray.Param("RA", Ra));
            paramArr.Add(NetworkArray.Param("CU", Cu));
            paramArr.Add(NetworkArray.Param("M", M));
        }

        arr.Add(paramArr);
        return arr;
    }

    private string FixMountState(string value) {
        // raised pet geometry - set from Fp
        PetGeometryType GeometryType = PetGeometryType.Default;
        // raised pet age - set from Fp
        PetAge PetAge = PetAge.Adult;
        // raised pet mounted - set from Fp
        bool PetMounted = false;

        string[] array = value.Split('*');
        Dictionary<string, string> keyValPairs = new();
        foreach (string str in array) {
            string[] keyValPair = str.Split('$');
            if (keyValPair.Length == 2)
                keyValPairs[keyValPair[0]] = keyValPair[1];
        }
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
        if (PetMounted && !Configuration.ServerConfiguration.AllowChaos &&
            (GeometryType == PetGeometryType.Default && PetAge < PetAge.Teen
            || GeometryType == PetGeometryType.Terror && PetAge < PetAge.Titan)
        ) {
            return Regex.Replace(value, "^U\\$[01]\\*", "U$-1*");
        } else {
            return value;
        }
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
