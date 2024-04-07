using sodoffmmo.Management;

namespace sodoffmmo.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ManagementCommandAttribute : Attribute {
    public string Name { get; set; }
    public Role Role { get; set; }

    public ManagementCommandAttribute(string name, Role role) {
        Name = name;
        Role = role;
    }
}
