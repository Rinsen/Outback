using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Models;

namespace SampleServer.InMemoryAccessors
{
    public class SigningAccessor : ITokenSigningAccessor, IWellKnownSigningAccessor
    {
        private readonly EllipticCurveJsonWebKeyModel _ellipticCurveJsonWebKeyModel;
        private readonly JsonWebKey _key;
        private readonly ECDsaSecurityKey _ecdSaKey;
        private readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();

        public SigningAccessor()
        {
            var secret = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            _ecdSaKey = new ECDsaSecurityKey(secret)
            {
                KeyId = GetRandomString(20)
            };

            _key = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(_ecdSaKey);
            var publicKey = _ecdSaKey.ECDsa.ExportParameters(false);

            _ellipticCurveJsonWebKeyModel = new EllipticCurveJsonWebKeyModel(_ecdSaKey.KeyId, Base64UrlEncoder.Encode(publicKey.Q.X), Base64UrlEncoder.Encode(publicKey.Q.Y), JsonWebKeyECTypes.P256, SecurityAlgorithms.EcdsaSha256);
        }


        public Task<EllipticCurveJsonWebKeyModelKeys> GetEllipticCurveJsonWebKeyModelKeys()
        {
            return Task.FromResult(new EllipticCurveJsonWebKeyModelKeys { Keys = new List<EllipticCurveJsonWebKeyModel> { _ellipticCurveJsonWebKeyModel } });
        }

        public Task<SecurityKeyWithAlgorithm> GetSigningSecurityKey()
        {
            return Task.FromResult(new SecurityKeyWithAlgorithm(_key, SecurityAlgorithms.EcdsaSha256));
        }

        private string GetRandomString(int length)
        {
            var bytes = new byte[length];

            CryptoRandom.GetBytes(bytes);

            return Base64UrlTextEncoder.Encode(bytes);
        }
    }
}
