using sodoffmmo.Attributes;
using sodoffmmo.Core;
using sodoffmmo.Data;
using sodoffmmo.Management;

namespace sodoffmmo.CommandHandlers;

[CommandHandler(1)]
class LoginHandler : ICommandHandler
{
    public void Handle(Client client, NetworkObject receivedObject)
    {
        client.PlayerData.UNToken = receivedObject.Get<string>("un");
        if (!ValidToken(client)) {
            NetworkObject obj = new();
            obj.Add("dr", (byte)1);
            client.Send(NetworkObject.WrapObject(0, 1005, obj).Serialize());
            client.ScheduleDisconnect();
            return;
        }

        NetworkArray rl = new();

        NetworkArray r1 = new();
        r1.Add(0);
        r1.Add("MP_SYS");
        r1.Add("default");
        r1.Add(true);
        r1.Add(false);
        r1.Add(false);
        r1.Add((short)0);
        r1.Add((short)10);
        r1.Add(new NetworkArray());
        r1.Add((short)0);
        r1.Add((short)0);
        rl.Add(r1);

        NetworkArray r2 = new();
        r2.Add(1);
        r2.Add("ADMIN");
        r2.Add("default");
        r2.Add(false);
        r2.Add(false);
        r2.Add(true);
        r2.Add((short)0);
        r2.Add((short)1);
        r2.Add(WorldEvent.Get().EventInfoArray(true));
        rl.Add(r2);

        NetworkArray r3 = new();
        r3.Add(2);
        r3.Add("LIMBO");
        r3.Add("default");
        r3.Add(false);
        r3.Add(false);
        r3.Add(false);
        r3.Add((short)31);
        r3.Add((short)10000);
        r3.Add(new NetworkArray());
        rl.Add(r3);

        NetworkObject content = new();
        content.Add("rl", rl);
        content.Add("zn", "JumpStart");
        content.Add("rs", (short)5);
        content.Add("un", client.PlayerData.UNToken);
        content.Add("id", client.ClientID);
        content.Add("pi", (short)1);

        client.Send(NetworkObject.WrapObject(0, 1, content).Serialize());
    }

    private bool ValidToken(Client client) {
        if (!Configuration.ServerConfiguration.Authentication ||
            (client.PlayerData.UNToken == Configuration.ServerConfiguration.BypassToken && !string.IsNullOrEmpty(Configuration.ServerConfiguration.BypassToken)))
            return true;
        try {
            HttpClient httpClient = new();
            var content = new FormUrlEncodedContent(
                new Dictionary<string, string> {
                    { "token", client.PlayerData.UNToken },
            });

            httpClient.Timeout = new TimeSpan(0, 0, 3);
            var response = httpClient.PostAsync($"{Configuration.ServerConfiguration.ApiUrl}/Authentication/MMOAuthentication", content).Result;
            string? responseString = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return false;
            if (responseString == null)
                throw new Exception("Response string null");
            Console.WriteLine(responseString);
            AuthenticationInfo info = Utils.DeserializeXml<AuthenticationInfo>(responseString);
            if (info.Authenticated) {
                client.PlayerData.DiplayName = info.DisplayName;
                client.PlayerData.Role = info.Role;
                return true;
            }
        } catch (Exception ex) {
            Console.WriteLine($"Authentication exception IID: {client.ClientID} - {ex}");
        }
        return false;
    }
}
