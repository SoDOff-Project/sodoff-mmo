using System.Xml.Serialization;

namespace sodoffmmo.Management;

[Serializable]
public class AuthenticationInfo {
    [XmlElement]
    public bool Authenticated { get; set; }

    [XmlElement]
    public int Id { get; set; }

    [XmlElement]
    public string VikingName { get; set; }

    [XmlElement]
    public Role Role { get; set; }
}

[Serializable]
public enum Role {
    User = 0, Moderator = 1, Admin = 2
}
