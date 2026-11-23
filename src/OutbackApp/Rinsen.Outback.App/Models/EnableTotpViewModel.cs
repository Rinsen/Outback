using System;

namespace Rinsen.Outback.App.Models;

public class EnableTotpViewModel
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The standard <c>otpauth://</c> URI an authenticator app scans.
    /// Parameters (SHA1 / 6 digits / 30s period) match the backend's
    /// Otp.NET <c>new Totp(secret)</c> defaults.
    /// </summary>
    public string OtpauthUri
    {
        get
        {
            var issuer = Uri.EscapeDataString(Issuer);
            var label = Uri.EscapeDataString(Label);

            return $"otpauth://totp/{issuer}:{label}?secret={Secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";
        }
    }
}
