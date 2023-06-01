using System.Threading.Tasks;

namespace Rinsen.Outback.Configuration
{
    public interface IOutbackConfigurationAccessor
    {
        Task<bool> IsDeviceAuthorizationGrantActiveAsync();

        Task<bool> IsClientCredentialsGrantActiveAsync();

        Task<bool> IsRefreshTokenGrantActiveAsync();

        Task<bool> IsCodeGrantActiveAsync();

        Task<bool> IsClientSecretBasicAuthenticationActiveAsync();

        Task<bool> IsClientSecretPostAuthenticationActiveAsync();
    }
}
