using sodoffmmo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sodoffmmo.Management;
public interface IManagementCommand {
    public void Handle(Client client, string[] arguments);
}
