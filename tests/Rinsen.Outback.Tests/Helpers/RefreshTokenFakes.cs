using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.JwtTokens;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Tests.Helpers
{
    /// <summary>
    /// Returns a preconfigured refresh-token grant and records any rotated grant.
    /// Optionally throws from <see cref="GetRefreshTokenGrantAsync"/> to simulate a failed lookup.
    /// </summary>
    public class FakeGrantAccessor : ICodeGrantAccessor, IRefreshTokenGrantAccessor, IDeviceAuthorizationGrantAccessor
    {
        private readonly RefreshTokenGrant? _grant;
        private readonly Exception? _getRefreshTokenGrantException;
        private readonly DeviceCodeGrant? _deviceGrant;

        public RefreshTokenGrant? SavedGrant { get; private set; }

        /// <summary>
        /// Family ids passed to <see cref="RevokeRefreshTokenFamilyAsync"/>, in call order.
        /// </summary>
        public List<Guid> RevokedFamilyIds { get; } = new();

        /// <summary>
        /// Device codes passed to <see cref="ResolveDeviceAuthorizationGrantAsync"/>, in call order.
        /// </summary>
        public List<string> ResolvedDeviceCodes { get; } = new();

        public FakeGrantAccessor(RefreshTokenGrant? grant, Exception? getRefreshTokenGrantException = null, DeviceCodeGrant? deviceGrant = null)
        {
            _grant = grant;
            _getRefreshTokenGrantException = getRefreshTokenGrantException;
            _deviceGrant = deviceGrant;
        }

        public Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken)
        {
            if (_getRefreshTokenGrantException is not null)
            {
                throw _getRefreshTokenGrantException;
            }

            if (_grant is null)
            {
                throw new InvalidOperationException("No refresh token grant configured.");
            }

            return Task.FromResult(_grant);
        }

        public Task SaveRefreshTokenGrantAsync(RefreshTokenGrant refreshTokenGrant)
        {
            SavedGrant = refreshTokenGrant;

            return Task.CompletedTask;
        }

        public Task RevokeRefreshTokenFamilyAsync(Guid familyId)
        {
            RevokedFamilyIds.Add(familyId);

            return Task.CompletedTask;
        }

        public Task<Rinsen.Outback.Grants.AuthorizationCodeGrant> GetCodeGrantAsync(string code) => throw new NotSupportedException();

        public Task<PersistedGrant> GetPersistedGrantAsync(string clientId, string subjectId) => throw new NotSupportedException();

        public Task<DeviceCodeGrant> GetDeviceAuthorizationGrantAsync(string deviceCode)
        {
            if (_deviceGrant is null)
            {
                throw new InvalidOperationException("No device authorization grant configured.");
            }

            return Task.FromResult(_deviceGrant);
        }

        public Task SaveCodeGrantAsync(Rinsen.Outback.Grants.AuthorizationCodeGrant codeGrant) => throw new NotSupportedException();

        public Task SavePersistedGrantAsync(PersistedGrant persistedGrant) => throw new NotSupportedException();

        public Task SaveDeviceAuthorizationGrantAsync(DeviceCodeGrant deviceAuthorizationGrant) => throw new NotSupportedException();

        public Task ResolveDeviceAuthorizationGrantAsync(string deviceCode)
        {
            ResolvedDeviceCodes.Add(deviceCode);

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Returns a canned access-token response for the refresh-token flow.
    /// </summary>
    public class FakeTokenService : ITokenService
    {
        public Task<Rinsen.Outback.Models.AccessTokenResponse> CreateTokenResponseAsync(Client client, RefreshTokenGrant refreshTokenGrant, string refreshToken, string issuer)
        {
            return Task.FromResult(new Rinsen.Outback.Models.AccessTokenResponse
            {
                AccessToken = "TestAccessToken",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                RefreshToken = refreshToken,
            });
        }

        public Task<Rinsen.Outback.Models.AccessTokenResponse> CreateTokenResponseAsync(Client client, string issuer) => throw new NotSupportedException();

        public Task<Rinsen.Outback.Models.AccessTokenResponse> CreateTokenResponseAsync(Client client, DeviceCodeGrant deviceAuthorizationGrant, string issuer)
        {
            return Task.FromResult(new Rinsen.Outback.Models.AccessTokenResponse
            {
                AccessToken = "TestAccessToken",
                TokenType = "Bearer",
                ExpiresIn = 3600,
            });
        }

        public Task<Rinsen.Outback.Models.AccessTokenResponse> CreateTokenResponseAsync(Client client, Rinsen.Outback.Grants.AuthorizationCodeGrant persistedGrant, string issuer) => throw new NotSupportedException();

        public Task<Rinsen.Outback.Models.AccessTokenResponse> CreateTokenResponseAsync(Client client, Rinsen.Outback.Grants.AuthorizationCodeGrant persistedGrant, string refreshToken, string issuer) => throw new NotSupportedException();
    }
}
