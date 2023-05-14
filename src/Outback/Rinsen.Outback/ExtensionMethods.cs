using System;
using Microsoft.AspNetCore.Identity;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Configuration;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.JwtTokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class ExtensionMethods
{
    public static void AddRinsenOutback(this IServiceCollection services, Action<OutbackOptions> outbackOptionsAction)
    {
        var outbackOptions = new OutbackOptions();

        outbackOptionsAction.Invoke(outbackOptions);

        services.AddSingleton(outbackOptions);
        services.AddSingleton<RandomStringGenerator>();
        services.AddScoped<IGrantService, GrantService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<ITokenService, TokenService>();

        if (outbackOptions.UseDefaultConfigurationAccessor)
        {
            services.AddSingleton<IOutbackConfigurationAccessor, DefaultOutbackConfigurationAccessor>();
        }
    }

    public static IMvcBuilder AddRinsenOutbackControllers(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.AddApplicationPart(typeof(Client).Assembly);

        return mvcBuilder;
    }
}
