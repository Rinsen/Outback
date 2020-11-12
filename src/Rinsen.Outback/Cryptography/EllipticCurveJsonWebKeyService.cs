using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Rinsen.Outback.Cryptography
{
    public class EllipticCurveJsonWebKeyService
    {
        private readonly EllipticCurveJsonWebKeyModel _ellipticCurveJsonWebKeyModel;
        private readonly ECDsaSecurityKey _key;

        public EllipticCurveJsonWebKeyService(RandomStringGenerator randomStringGenerator)
        {
            var secret = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            _key = new ECDsaSecurityKey(secret)
            {
                KeyId = randomStringGenerator.GetRandomString(20)
            };

            var publicKey = _key.ECDsa.ExportParameters(false);

            _ellipticCurveJsonWebKeyModel = new EllipticCurveJsonWebKeyModel
            {
                Curve = JsonWebKeyECTypes.P256,
                KeyId = _key.KeyId,
                X = Base64UrlEncoder.Encode(publicKey.Q.X),
                Y = Base64UrlEncoder.Encode(publicKey.Q.Y),
                SigningAlgorithm = SecurityAlgorithms.EcdsaSha256
            };
        }

        public ECDsaSecurityKey GetECDsaSecurityKey()
        {
            return _key;
        }

        public EllipticCurveJsonWebKeyModel GetEllipticCurveJsonWebKeyModel()
        {
            return _ellipticCurveJsonWebKeyModel;
        }

    }
}
