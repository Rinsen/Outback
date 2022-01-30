namespace Rinsen.Outback.Clients;

public class ClientIdentity
{
    public ClientIdentity(string username, string password)
    {
        ClientId = username;
        Secret = password;
    }

    public string Secret { get; }
    public string ClientId { get; }
}
