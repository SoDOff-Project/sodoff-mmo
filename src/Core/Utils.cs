using sodoffmmo.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sodoffmmo.Data;

namespace sodoffmmo.Core;
internal static class Utils {
    public static NetworkPacket VlNetworkPacket(NetworkArray vl, int roomID) {
        NetworkObject obj = new();
        obj.Add("r", roomID);
        obj.Add("vl", vl);
        return NetworkObject.WrapObject(0, 11, obj).Serialize();
    }

    public static NetworkPacket VlNetworkPacket(string a, string b, int roomID) {
        NetworkArray vl = new();
        vl.Add(NetworkArray.VlElement(a, b));
        return VlNetworkPacket(vl, roomID);
    }

    public static NetworkPacket ArrNetworkPacket(string[] data, string c = "", int? roomID = null) {
        NetworkObject cmd = new();
        NetworkObject obj = new();
        obj.Add("arr", data);
        cmd.Add("c", c);
        cmd.Add("p", obj);
        NetworkObject ret = NetworkObject.WrapObject(1, 13, cmd);
        if (roomID != null)
            ret.Add("r", (int)roomID);
        return ret.Serialize();
    }
}
