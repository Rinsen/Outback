using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Rinsen.Outback.Models;

public sealed class AdditionalTokenParametersBinder : IModelBinder
{
    // All known token endpoint parameters handled by TokenModel
    private static readonly HashSet<string> KnownKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "grant_type",
        "code",
        "refresh_token",
        "redirect_uri",
        "client_id",
        "client_secret",
        "code_verifier",
        "device_code",
    };

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext is null) throw new ArgumentNullException(nameof(bindingContext));

        var request = bindingContext.HttpContext.Request;

        if (!request.HasFormContentType)
        {
            bindingContext.Result = ModelBindingResult.Success(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            return Task.CompletedTask;
        }

        var form = request.Form;
        var extras = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in form)
        {
            if (!KnownKeys.Contains(kvp.Key))
            {
                // If multiple values are present, ToString() joins them with commas.
                // If you need multi-valued support, switch Dictionary<string, string> to Dictionary<string, StringValues>.
                extras[kvp.Key] = kvp.Value.ToString();
            }
        }

        bindingContext.Result = ModelBindingResult.Success(extras);

        return Task.CompletedTask;
    }
}
