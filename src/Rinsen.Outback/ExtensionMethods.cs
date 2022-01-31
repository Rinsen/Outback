using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.JwtTokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class ExtensionMethods
{
    public static void AddRinsenOutback(this IServiceCollection services)
    {
        services.AddSingleton<RandomStringGenerator>();
        services.AddScoped<IGrantService, GrantService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<ITokenService, TokenService>();
    }

    public static IMvcBuilder AddRinsenOutbackControllers(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.AddApplicationPart(typeof(Client).Assembly);

        return mvcBuilder;
    }
}
