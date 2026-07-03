// Copyright 2019 Ipregistry (https://ipregistry.co).
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Text.Json.Serialization;

namespace Ipregistry;

/// <summary>
/// Holds the comprehensive set of information associated with an IP address
/// returned by the Ipregistry API.
/// </summary>
/// <remarks>
/// Nested objects (<see cref="Carrier"/>, <see cref="Company"/>, <see cref="Connection"/>,
/// <see cref="Currency"/>, <see cref="Location"/>, <see cref="Security"/>, and
/// <see cref="TimeZone"/>) are always non-null, so accessing their properties never
/// throws even when the API omitted them; absent fields hold their default value.
/// </remarks>
public record IpInfo
{
    /// <summary>The IP address the data refers to.</summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }

    /// <summary>The IP version: <see cref="IpType.IPv4"/>, <see cref="IpType.IPv6"/>, or <see cref="IpType.Unknown"/>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The reverse-DNS hostname, when hostname resolution is requested
    /// (see <see cref="LookupOptions.Hostname"/>) and available.
    /// </summary>
    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    /// <summary>Mobile carrier information.</summary>
    [JsonPropertyName("carrier")]
    public Carrier Carrier { get; set; } = new();

    /// <summary>Ownership information for the IP address.</summary>
    [JsonPropertyName("company")]
    public Company Company { get; set; } = new();

    /// <summary>Network connection information.</summary>
    [JsonPropertyName("connection")]
    public Connection Connection { get; set; } = new();

    /// <summary>Currency information for the IP address location.</summary>
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; } = new();

    /// <summary>Geographical location.</summary>
    [JsonPropertyName("location")]
    public Location Location { get; set; } = new();

    /// <summary>Threat-intelligence flags.</summary>
    [JsonPropertyName("security")]
    public Security Security { get; set; } = new();

    /// <summary>Time zone information for the IP address location.</summary>
    [JsonPropertyName("time_zone")]
    public TimeZone TimeZone { get; set; } = new();
}

/// <summary>
/// Enriches <see cref="IpInfo"/> with parsed User-Agent data. It is returned by
/// <see cref="IIpregistryClient.LookupOriginAsync"/>, where the User-Agent of the
/// calling client is known.
/// </summary>
public sealed record RequesterIpInfo : IpInfo
{
    /// <summary>The parsed User-Agent of the requester, or null when the API did not return any.</summary>
    [JsonPropertyName("user_agent")]
    public UserAgent? UserAgent { get; set; }
}

/// <summary>Well-known values of <see cref="IpInfo.Type"/>.</summary>
public static class IpType
{
    /// <summary>An IPv4 address.</summary>
    public const string IPv4 = "IPv4";

    /// <summary>An IPv6 address.</summary>
    public const string IPv6 = "IPv6";

    /// <summary>An address whose version could not be determined.</summary>
    public const string Unknown = "Unknown";
}

/// <summary>Holds mobile carrier information associated with an IP address.</summary>
public sealed record Carrier
{
    /// <summary>The carrier name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The Mobile Country Code.</summary>
    [JsonPropertyName("mcc")]
    public string? Mcc { get; set; }

    /// <summary>The Mobile Network Code.</summary>
    [JsonPropertyName("mnc")]
    public string? Mnc { get; set; }
}

/// <summary>Well-known values of <see cref="Company.Type"/>.</summary>
public static class CompanyType
{
    /// <summary>A business company.</summary>
    public const string Business = "business";

    /// <summary>An educational institution.</summary>
    public const string Education = "education";

    /// <summary>A government body.</summary>
    public const string Government = "government";

    /// <summary>A hosting provider.</summary>
    public const string Hosting = "hosting";

    /// <summary>An Internet service provider.</summary>
    public const string Isp = "isp";
}

/// <summary>Holds ownership information for the IP address.</summary>
public sealed record Company
{
    /// <summary>The company name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The company domain.</summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    /// <summary>The kind of company; see <see cref="CompanyType"/> for well-known values.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>Well-known values of <see cref="Connection.Type"/>.</summary>
public static class ConnectionType
{
    /// <summary>A business network.</summary>
    public const string Business = "business";

    /// <summary>An education network.</summary>
    public const string Education = "education";

    /// <summary>A government network.</summary>
    public const string Government = "government";

    /// <summary>A hosting network.</summary>
    public const string Hosting = "hosting";

    /// <summary>An inactive network.</summary>
    public const string Inactive = "inactive";

    /// <summary>An Internet service provider network.</summary>
    public const string Isp = "isp";
}

/// <summary>Holds network connection information for the IP address.</summary>
public sealed record Connection
{
    /// <summary>The Autonomous System Number, or null when unknown.</summary>
    [JsonPropertyName("asn")]
    public long? Asn { get; set; }

    /// <summary>The connection domain.</summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    /// <summary>The organization operating the network.</summary>
    [JsonPropertyName("organization")]
    public string? Organization { get; set; }

    /// <summary>The BGP route the IP address belongs to.</summary>
    [JsonPropertyName("route")]
    public string? Route { get; set; }

    /// <summary>The kind of network; see <see cref="ConnectionType"/> for well-known values.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>Holds currency information for the IP address location.</summary>
public sealed record Currency
{
    /// <summary>The ISO 4217 currency code (for example "USD").</summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>The currency name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The currency name in the local language.</summary>
    [JsonPropertyName("name_native")]
    public string? NameNative { get; set; }

    /// <summary>The plural form of the currency name.</summary>
    [JsonPropertyName("plural")]
    public string? Plural { get; set; }

    /// <summary>The plural form of the currency name in the local language.</summary>
    [JsonPropertyName("plural_native")]
    public string? PluralNative { get; set; }

    /// <summary>The currency symbol (for example "$").</summary>
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    /// <summary>The currency symbol in the local language.</summary>
    [JsonPropertyName("symbol_native")]
    public string? SymbolNative { get; set; }

    /// <summary>How monetary values are formatted for this currency.</summary>
    [JsonPropertyName("format")]
    public CurrencyFormat Format { get; set; } = new();
}

/// <summary>Describes how monetary values are formatted for a currency.</summary>
public sealed record CurrencyFormat
{
    /// <summary>The decimal separator character.</summary>
    [JsonPropertyName("decimal_separator")]
    public string? DecimalSeparator { get; set; }

    /// <summary>The digit-grouping separator character.</summary>
    [JsonPropertyName("group_separator")]
    public string? GroupSeparator { get; set; }

    /// <summary>The affixes applied around a negative monetary value.</summary>
    [JsonPropertyName("negative")]
    public CurrencyFormatAffix Negative { get; set; } = new();

    /// <summary>The affixes applied around a positive monetary value.</summary>
    [JsonPropertyName("positive")]
    public CurrencyFormatAffix Positive { get; set; } = new();
}

/// <summary>
/// Holds the prefix and suffix applied around a formatted monetary value
/// (for example the currency symbol and a sign).
/// </summary>
public sealed record CurrencyFormatAffix
{
    /// <summary>The prefix placed before the value.</summary>
    [JsonPropertyName("prefix")]
    public string? Prefix { get; set; }

    /// <summary>The suffix placed after the value.</summary>
    [JsonPropertyName("suffix")]
    public string? Suffix { get; set; }
}

/// <summary>Holds threat-intelligence flags for the IP address.</summary>
public sealed record Security
{
    /// <summary>Whether the IP address is a known source of abuse.</summary>
    [JsonPropertyName("is_abuser")]
    public bool IsAbuser { get; set; }

    /// <summary>Whether the IP address is a known source of attacks.</summary>
    [JsonPropertyName("is_attacker")]
    public bool IsAttacker { get; set; }

    /// <summary>Whether the IP address is a bogon (unallocated or reserved).</summary>
    [JsonPropertyName("is_bogon")]
    public bool IsBogon { get; set; }

    /// <summary>Whether the IP address belongs to a cloud provider.</summary>
    [JsonPropertyName("is_cloud_provider")]
    public bool IsCloudProvider { get; set; }

    /// <summary>Whether the IP address is a known proxy.</summary>
    [JsonPropertyName("is_proxy")]
    public bool IsProxy { get; set; }

    /// <summary>Whether the IP address is a known relay (for example iCloud Private Relay).</summary>
    [JsonPropertyName("is_relay")]
    public bool IsRelay { get; set; }

    /// <summary>Whether the IP address is a Tor node.</summary>
    [JsonPropertyName("is_tor")]
    public bool IsTor { get; set; }

    /// <summary>Whether the IP address is a Tor exit node.</summary>
    [JsonPropertyName("is_tor_exit")]
    public bool IsTorExit { get; set; }

    /// <summary>Whether the IP address anonymizes its user (proxy, relay, Tor, or VPN).</summary>
    [JsonPropertyName("is_anonymous")]
    public bool IsAnonymous { get; set; }

    /// <summary>Whether the IP address is considered a threat.</summary>
    [JsonPropertyName("is_threat")]
    public bool IsThreat { get; set; }

    /// <summary>Whether the IP address is a known VPN endpoint.</summary>
    [JsonPropertyName("is_vpn")]
    public bool IsVpn { get; set; }
}

/// <summary>Holds time zone information for the IP address location.</summary>
public sealed record TimeZone
{
    /// <summary>The IANA time zone identifier (for example "America/Los_Angeles").</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>The time zone abbreviation (for example "PST").</summary>
    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }

    /// <summary>The current local time as an ISO 8601 string.</summary>
    [JsonPropertyName("current_time")]
    public string? CurrentTime { get; set; }

    /// <summary>The time zone name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The current offset from UTC in seconds.</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>Whether daylight saving time is currently in effect.</summary>
    [JsonPropertyName("in_daylight_saving")]
    public bool InDaylightSaving { get; set; }
}
