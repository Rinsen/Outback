using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Rinsen.Outback.Accessors;

public interface ITokenSigningAccessor
{
    Task<SecurityKeyWithAlgorithm> GetSigningSecurityKey();
}

public class SecurityKeyWithAlgorithm
{
    public SecurityKeyWithAlgorithm(SecurityKey securityKey, string algorithm)
    {
        SecurityKey = securityKey;
        Algorithm = algorithm;
    }

    public SecurityKey SecurityKey { get; }

    public string Algorithm { get; }

}
