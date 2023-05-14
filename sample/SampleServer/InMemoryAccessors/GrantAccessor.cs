using System.Collections.Concurrent;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Grants;

namespace SampleServer.InMemoryAccessors
{
    public class GrantAccessor : IGrantAccessor
    {
        private readonly ConcurrentDictionary<string, CodeGrant> _persistedGrants = new();
        private readonly ConcurrentDictionary<string, DeviceAuthorizationGrant> _persistedDeviceAuthorizationGrant = new();

        public Task<CodeGrant> GetCodeGrantAsync(string code)
        {
            var codeGrant = _persistedGrants.GetValueOrDefault(code);

            if (codeGrant == default)
            {
                throw new Exception("CodeGrant not found");
            }

            return Task.FromResult(codeGrant);
        }

        public Task<DeviceAuthorizationGrant> GetDeviceAuthorizationGrantAsync(string deviceCode)
        {
            var deviceGrant = _persistedDeviceAuthorizationGrant.GetValueOrDefault(deviceCode);

            if (deviceGrant == default)
            {
                throw new Exception("Device grant not found");
            }

            return Task.FromResult(deviceGrant);
        }

        public Task<PersistedGrant> GetPersistedGrantAsync(string clientId, string subjectId)
        {
            throw new NotImplementedException();
        }

        public Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken)
        {
            throw new NotImplementedException();
        }

        public Task SaveCodeGrantAsync(CodeGrant codeGrant)
        {
            _persistedGrants.TryAdd(codeGrant.Code, codeGrant);

            return Task.CompletedTask;
        }

        public Task SaveDeviceAuthorizationGrantAsync(DeviceAuthorizationGrant deviceAuthorizationGrant)
        {
            _persistedDeviceAuthorizationGrant.TryAdd(deviceAuthorizationGrant.DeviceCode, deviceAuthorizationGrant);

            return Task.CompletedTask;
        }

        public Task SavePersistedGrantAsync(PersistedGrant persistedGrant)
        {
            throw new NotImplementedException();
        }

        public Task SaveRefreshTokenGrantAsync(RefreshTokenGrant refreshTokenGrant)
        {
            throw new NotImplementedException();
        }
    }
}
