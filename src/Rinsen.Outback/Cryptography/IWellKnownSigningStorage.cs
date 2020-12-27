using System.Threading.Tasks;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Cryptography
{
    public interface IWellKnownSigningStorage
    {
        Task<EllipticCurveJsonWebKeyModelKeys> GetEllipticCurveJsonWebKeyModelKeys();

    }
}
