using System;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Configuration;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.JwtTokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class ExtensionMethods
{
    public static void AddRinsenOutback(this IServiceCollection services)
    {
        var outbackOptions = new OutbackOptions();

        services.AddSingleton(outbackOptions);
        AddServices(services);

        if (outbackOptions.UseDefaultConfigurationAccessor)
        {
            services.AddSingleton<IOutbackConfigurationAccessor, DefaultOutbackConfigurationAccessor>();
        }
    }

    public static void AddRinsenOutback(this IServiceCollection services, Action<OutbackOptions> outbackOptionsAction)
    {
        var outbackOptions = new OutbackOptions();

        outbackOptionsAction.Invoke(outbackOptions);

        services.AddSingleton(outbackOptions);
        AddServices(services);

        if (outbackOptions.UseDefaultConfigurationAccessor)
        {
            services.AddSingleton<IOutbackConfigurationAccessor, DefaultOutbackConfigurationAccessor>();
        }
    }

    private static void AddServices(IServiceCollection services)
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
