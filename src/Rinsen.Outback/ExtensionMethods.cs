using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;

namespace Rinsen.Outback
{
    public static class ExtensionMethods
    {
        public static void AddRinsenOutback(this IServiceCollection services)
        {
            services.AddSingleton<RandomStringGenerator>();
            services.AddScoped<GrantService>();
            services.AddScoped<ClientService>();
            services.AddScoped<TokenFactory>();
        }

        public static IMvcBuilder AddRinsenOutbackControllers(this IMvcBuilder mvcBuilder)
        {
            mvcBuilder.AddApplicationPart(typeof(Client).Assembly);

            return mvcBuilder;
        }
    }
}
