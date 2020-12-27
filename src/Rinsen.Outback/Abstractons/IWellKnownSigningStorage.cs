using System.Threading.Tasks;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Abstractons
{
    public interface IWellKnownSigningStorage
    {
        Task<EllipticCurveJsonWebKeyModelKeys> GetEllipticCurveJsonWebKeyModelKeys();

    }
}
