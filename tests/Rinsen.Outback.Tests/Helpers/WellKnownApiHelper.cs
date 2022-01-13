using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.Outback.Tests.Helpers
{
    public static class WellKnownApiHelper
    {

        public static async Task<OpenIdConfigurationModel> GetOpenIdConfiguration(this HttpClient client)
        {
            var response = await client.GetAsync(".well-known/openid-configuration");
            response.EnsureSuccessStatusCode();

            var openIdConfiguration = await response.Content.ReadFromJsonAsync<OpenIdConfigurationModel>();

            if (openIdConfiguration == null)
            {
                throw new Exception();
            }

            return openIdConfiguration;
        }

        public static async Task<JsonWebKeyModel> GetJsonWebKey(this HttpClient client)
        {
            var response = await client.GetAsync(".well-known/openid-configuration/jwks");
            response.EnsureSuccessStatusCode();

            var jsonWebKeyModelKeys = await response.Content.ReadFromJsonAsync<JsonWebKeyModelKeys>();

            if (jsonWebKeyModelKeys == null)
            {
                throw new Exception();
            }

            return jsonWebKeyModelKeys.Keys.Single();
        }
    }
}
