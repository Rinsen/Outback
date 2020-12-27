using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Rinsen.Outback.Abstractons
{
    public interface ITokenSigningStorage
    {
        Task<SecurityKey> GetSigningSecurityKey();
        Task<string> GetSigningAlgorithm();
    }
}
