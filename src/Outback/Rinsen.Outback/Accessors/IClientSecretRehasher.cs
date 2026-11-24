using System.Threading.Tasks;

namespace Rinsen.Outback.Accessors;

/// <summary>
/// Optional capability for accessors backed by a persistent store. Invoked after a
/// legacy-format (unsalted) stored client secret has been verified successfully, so
/// the plaintext — which is only available at verification time — is re-stored with
/// the salted KDF (SEC-013). Accessors without persistence (e.g. in-memory test
/// fakes) simply do not implement this interface.
/// </summary>
public interface IClientSecretRehasher
{
    /// <summary>
    /// Replaces the client's stored secret value <paramref name="oldStored"/> with
    /// <paramref name="newStored"/> (a fresh salted hash of the same plaintext).
    /// </summary>
    Task RehashClientSecretAsync(string clientId, string oldStored, string newStored);
}
