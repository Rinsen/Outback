using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Rinsen.Outback.Cryptography
{
    public interface ITokenSigningStorage
    {
        Task<SecurityKey> GetSigningSecurityKey();
        Task<string> GetSigningAlgorithm();
    }
}
