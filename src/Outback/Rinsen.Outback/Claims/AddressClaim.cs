﻿using System.Text.Json.Serialization;

namespace Rinsen.Outback.Claims;

public class AddressClaim
{
    /// <summary>
    /// Full mailing address, formatted for display or use on a mailing label.This field MAY contain multiple lines, separated by newlines.Newlines can be represented either as a carriage return/line feed pair ("\r\n") or as a single line feed character("\n").
    /// </summary>
    [JsonPropertyName("formatted")]
    public string Formatted { get; set; } = string.Empty;

    /// <summary>
    /// Full street address component, which MAY include house number, street name, Post Office Box, and multi-line extended street address information.This field MAY contain multiple lines, separated by newlines.Newlines can be represented either as a carriage return/line feed pair ("\r\n") or as a single line feed character("\n").
    /// </summary>
    [JsonPropertyName("street_address")]
    public string StreetAddress { get; set; } = string.Empty;

    /// <summary>
    /// City or locality component.
    /// </summary>
    [JsonPropertyName("locality")]
    public string Locality { get; set; } = string.Empty;

    /// <summary>
    /// State, province, prefecture, or region component.
    /// </summary>
    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Zip code or postal code component.
    /// </summary>
    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country name component.
    /// </summary>
    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

}
