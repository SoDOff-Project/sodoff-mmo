using System.Xml.Serialization;

namespace sodoffmmo.Management;

[Serializable]
public class AuthenticationInfo {
    [XmlElement]
    public bool Authenticated { get; set; }

    [XmlElement]
    public string DisplayName { get; set; }

    [XmlElement]
    public Role Role { get; set; }
}

[Serializable]
public enum Role {
    User, Admin, Moderator
}