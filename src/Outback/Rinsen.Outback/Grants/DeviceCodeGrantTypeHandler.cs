using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.JwtTokens;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Grants
{
    internal class DeviceCodeGrantTypeHandler : IGrantTypeHandler
    {
        private readonly IGrantAccessor _grantAccessor;
        private readonly ITokenService _tokenService;
        private readonly RandomStringGenerator _randomStringGenerator;
        private readonly ILogger<DeviceCodeGrantTypeHandler> _logger;

        public DeviceCodeGrantTypeHandler(IGrantAccessor grantAccessor,
            ITokenService tokenService,
            RandomStringGenerator randomStringGenerator,
            ILogger<DeviceCodeGrantTypeHandler> logger)
        {
            _grantAccessor = grantAccessor;
            _tokenService = tokenService;
            _randomStringGenerator = randomStringGenerator;
            _logger = logger;
        }

        public string GrantType => "urn:ietf:params:oauth:grant-type:device_code";

        public async Task<TokenResponse> GetTokenAsync(TokenModel tokenModel, Client client, string issuer)
        {
            var deviceGrant = await _grantAccessor.GetDeviceAuthorizationGrantAsync(tokenModel.DeviceCode);

            if (client.ClientId != deviceGrant.ClientId)
            {
                _logger.LogError("Device authorization grant {DeviceCode} is not for client {ClientId}", tokenModel.DeviceCode, client.ClientId);

                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.InvalidRequest } };
            }

            if (deviceGrant.AccessIsRejected)
            {
                _logger.LogInformation("Device authorization grant {DeviceCode} is rejected", tokenModel.DeviceCode);

                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.AccessDenied } };
            }

            if (DateTimeOffset.UtcNow.Subtract(deviceGrant.UserCodeExpiration).TotalMilliseconds > 0)
            {
                _logger.LogError("Device authorization grant {DeviceCode} have expired", tokenModel.DeviceCode);

                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.ExpiredToken } };
            }

            if (string.IsNullOrEmpty(deviceGrant.SubjectId))
            {
                _logger.LogInformation("Device authorization grant {DeviceCode} is not accepted yet", tokenModel.DeviceCode);

                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.AuthorizationPending } };
            }

            var tokenResponse = await _tokenService.CreateTokenResponseAsync(client, deviceGrant, issuer);

            return new TokenResponse { AccessTokenResponse = tokenResponse };
        }

        public async Task<DeviceCodeGrant> GetDeviceCodeGrantAsync(Client client, string scope)
        {
            var deviceCode = _randomStringGenerator.GetRandomString(40);
            var userCode = GetUserCode(12);

            var deviceAuthorizationGrant = new DeviceCodeGrant
            {
                ClientId = client.ClientId,
                DeviceCode = deviceCode,
                Interval = 5,
                SubjectId = null,
                Scope = scope,
                UserCode = userCode,
                UserCodeExpiration = DateTimeOffset.UtcNow.AddSeconds(client.DeviceCodeUserCompletionLifetime)
            };

            await _grantAccessor.SaveDeviceAuthorizationGrantAsync(deviceAuthorizationGrant);

            return deviceAuthorizationGrant;
        }

        private static string GetUserCode(int length)
        {
            const string sourceString = "BCDFGHJKLMNPQRSTVWXZ";

            byte[] randomBytes = new byte[length];

            using var randomNumberGenerator = RandomNumberGenerator.Create();

            randomNumberGenerator.GetBytes(randomBytes);

            var result = new StringBuilder(length + 2);

            var count = 0;
            foreach (byte b in randomBytes)
            {
                if (count == 4 || count == 9)
                {
                    result.Append('-');
                    count++;
                }

                result.Append(sourceString[b % sourceString.Length]);
                count++;
            }

            return result.ToString();
        }
    }
}
