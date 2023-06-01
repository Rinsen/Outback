using System.Threading.Tasks;

namespace Rinsen.Outback.Configuration
{
    internal class DefaultOutbackConfigurationAccessor : IOutbackConfigurationAccessor
    {
        private readonly OutbackOptions _outbackOptions;

        public DefaultOutbackConfigurationAccessor(OutbackOptions outbackOptions)
        {
            _outbackOptions = outbackOptions;
        }

        public Task<bool> IsDeviceAuthorizationGrantActiveAsync()
        {
            return Task.FromResult(_outbackOptions.DeviceAuthorizationGrantActive);
        }

        public Task<bool> IsClientCredentialsGrantActiveAsync()
        {
            return Task.FromResult(_outbackOptions.ClientCredentialsGrantActive);
        }

        public Task<bool> IsRefreshTokenGrantActiveAsync()
        {
            return Task.FromResult(_outbackOptions.RefreshTokenGrantActive);
        }

        public Task<bool> IsCodeGrantActiveAsync()
        {
            return Task.FromResult(_outbackOptions.CodeGrantActive);
        }

        public Task<bool> IsClientSecretBasicAuthenticationActiveAsync()
        {
            return Task.FromResult(_outbackOptions.ClientSecretBasicActive);
        }

        public Task<bool> IsClientSecretPostAuthenticationActiveAsync()
        {
            return Task.FromResult(_outbackOptions.ClientSecretPostActive);
        }
    }
}
