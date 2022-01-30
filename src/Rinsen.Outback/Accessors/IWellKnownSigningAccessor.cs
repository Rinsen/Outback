using System.Threading.Tasks;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Accessors;

public interface IWellKnownSigningAccessor
{
    Task<EllipticCurveJsonWebKeyModelKeys> GetEllipticCurveJsonWebKeyModelKeys();

}
