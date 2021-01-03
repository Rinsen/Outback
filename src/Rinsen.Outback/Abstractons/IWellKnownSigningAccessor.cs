using System.Threading.Tasks;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Abstractons
{
    public interface IWellKnownSigningAccessor
    {
        Task<EllipticCurveJsonWebKeyModelKeys> GetEllipticCurveJsonWebKeyModelKeys();

    }
}
