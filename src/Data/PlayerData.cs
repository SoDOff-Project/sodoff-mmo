using System.Text.RegularExpressions;
using System.Globalization;
using sodoffmmo.Core;
using sodoffmmo.Management;

namespace sodoffmmo.Data;
public class PlayerData {
    public bool IsValid { get; set; } = false;

    // viking uid
    public string Uid { get; set; } = "";
    // client token
    public string UNToken { get; set; } = "";

    // rotation (eulerAngles.y)
    public float R { get; set; }
    // velocity x
    public float R1 { get; set; }
    // velocity y
    public float R2 { get; set; }
    // velocity z
    public float R3 { get; set; }
    // position x
    public float P1 { get; set; }
    // position y
    public float P2 { get; set; }
    // position z
    public float P3 { get; set; }
    // max speed
    public float Mx { get; set; } = 6;
    // flags (?)
    public int F { get; set; }
    // animation bitfield (animations used by avatar, e.g. mounted, swim, ...)
    public int Mbf { get; set; }

    public static readonly string[] SupportedVariables = {
        "A",   // avatar data
        "FP",  // raised pet data
        "RA",  // XP rank (points and level)
        "UDT", // UDT points
        "L",   // location (level)
        "PU",  // (not raised) pet data
        "RDE", // ride (int)
        "MU",  // mood
        "MBR", // mount broom (bool)
        "GU",  // goggles (bool)
        "LC",  // livechat (int)
        "CU",  // country id (int) (for flag?)
        "J",   // join allowed
        "BU",  // busy (?)
        "M"    // membership status (bool)
    };

    // other variables (set and updated via SUV command)
    private Dictionary<string, string?> variables = new();

    public string GetVariable(string varName) {
        return variables[varName];
    }

    public void SetVariable(string varName, string value) {
        if (varName == "UID")
            return;
        if (varName == "FP")
            value = FixMountState(value);
        variables[varName] = value;
    }

    public void InitFromNetworkData(NetworkObject suvData) {
        // set initial state for SPV data
        R = float.Parse(suvData.Get<string>("R"), CultureInfo.InvariantCulture);
        P1 = float.Parse(suvData.Get<string>("P1"), CultureInfo.InvariantCulture);
        P2 = float.Parse(suvData.Get<string>("P2"), CultureInfo.InvariantCulture);
        P3 = float.Parse(suvData.Get<string>("P3"), CultureInfo.InvariantCulture);
        R1 = float.Parse(suvData.Get<string>("R1"), CultureInfo.InvariantCulture);
        R2 = float.Parse(suvData.Get<string>("R2"), CultureInfo.InvariantCulture);
        R3 = float.Parse(suvData.Get<string>("R3"), CultureInfo.InvariantCulture);
        string? mbf = suvData.Get<string>("MBF");
        if (mbf != null)
            Mbf = int.Parse(mbf);
        F = int.Parse(suvData.Get<string>("F"));

        // reset all variables values
        variables.Clear();

        // set initial state for SUV data
        foreach (string varName in SupportedVariables) {
            string? value = suvData.Get<string>(varName);
            if (value != null) {
                SetVariable(varName, value);
            }
        }

        IsValid = true;
    }

    public string DiplayName { get; set; } = "";
    public Role Role { get; set; } = Role.User;

    public NetworkArray GetNetworkData(int clientID, out NetworkArray paramArr) {
        NetworkArray arr = new();
        arr.Add(clientID);
        arr.Add(Uid);
        arr.Add((short)1);
        arr.Add((short)clientID);

        paramArr = new();
        paramArr.Add(NetworkArray.Param("NT", (double)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))); // network time (like PNG)
        paramArr.Add(NetworkArray.Param("t", (int)(Runtime.CurrentRuntime / 1000))); // timestamp (non-decreasing integer)

        paramArr.Add(NetworkArray.Param("UID", Uid));
        addVariableToArray(paramArr, "A");
        addVariableToArray(paramArr, "FP");

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
            
            foreach (var v in variables) {
                if (v.Value is null || v.Key == "A" || v.Key == "FP")
                    continue;
                paramArr.Add(NetworkArray.Param(v.Key, v.Value));
            }
        }

        arr.Add(paramArr);
        return arr;
    }

    private void addVariableToArray(NetworkArray paramArr, string varName) {
        if (variables.TryGetValue (varName, out string tmp) && tmp != null) {
            paramArr.Add(NetworkArray.Param(varName, tmp));
        }
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
