using sodoffmmo.Core;
using sodoffmmo.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace sodoffmmo.Module;

public class DummyClient : Client {
    public DummyClient() : base(null) { }

    public override void Send(NetworkPacket packet) { }
    public override Task Receive() => Task.Delay(Timeout.Infinite);
    public override void Disconnect() { }
    public override bool Connected => true;
}
