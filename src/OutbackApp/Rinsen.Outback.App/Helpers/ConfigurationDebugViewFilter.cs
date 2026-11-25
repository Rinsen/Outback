using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Rinsen.Outback.App.Helpers;

/// <summary>
/// SEC-023: masks sensitive configuration entries (connection strings, passwords,
/// secrets, invitation codes) before the debug view is returned to a client.
/// </summary>
public static class ConfigurationDebugViewFilter
{
    private static readonly string[] SensitiveTokens =
    {
        "connectionstring",
        "password",
        "secret",
        "invitationcode",
    };

    /// <summary>
    /// Returns the configuration debug view with sensitive entries removed:
    /// values under a sensitive key are masked, and lines whose own key is
    /// sensitive are dropped, so neither the key nor its value reaches the response.
    /// </summary>
    public static string GetFilteredDebugView(IConfigurationRoot root)
    {
        // Mask values whose path touches a sensitive key (this also covers the
        // children of a sensitive section), then drop the lines whose own key
        // is sensitive so the keys themselves are absent from the view.
        var view = root.GetDebugView(MaskValue);

        var lines = view
            .Split('\n')
            .Where(IsLineSafe)
            .Select(line => line.TrimEnd('\r'));

        return string.Join(Environment.NewLine, lines);
    }

    internal static bool IsSensitive(string key)
    {
        return SensitiveTokens.Any(token => key.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string MaskValue(ConfigurationDebugViewContext context)
    {
        return IsSensitive(context.Path) ? null! : context.Value ?? string.Empty;
    }

    private static bool IsLineSafe(string line)
    {
        var trimmed = line.TrimStart();

        // A line is either "Key=value (Provider)" or "Key:"; the key is the part
        // before the first '=' or ':'. Blank lines carry no key and are kept.
        var separator = trimmed.IndexOfAny(new[] { '=', ':' });
        if (separator <= 0)
            return true;

        return !IsSensitive(trimmed[..separator]);
    }
}
