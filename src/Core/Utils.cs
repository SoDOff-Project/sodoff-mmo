using sodoffmmo.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

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

    public static T DeserializeXml<T>(string xmlString) {
        var serializer = new XmlSerializer(typeof(T));
        using (var reader = new StringReader(xmlString))
            return (T)serializer.Deserialize(reader);
    }

    public static NetworkPacket BuildChatMessage(string uid, string message, string displayName) {
        NetworkObject cmd = new();
        NetworkObject data = new();
        data.Add("arr", new string[] { "CMR", "-1", uid, "1", message, "", "1", displayName });
        cmd.Add("c", "CMR");
        cmd.Add("p", data);

        return NetworkObject.WrapObject(1, 13, cmd).Serialize();
    }

    public static NetworkPacket BuildServerSideMessage(string message, string displayName) {
        return BuildChatMessage("-1", message, displayName);
    }

}
