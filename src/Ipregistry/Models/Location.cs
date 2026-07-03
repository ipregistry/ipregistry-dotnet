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

/// <summary>Holds the geographical location associated with an IP address.</summary>
public sealed record Location
{
    /// <summary>Continent-level information.</summary>
    [JsonPropertyName("continent")]
    public Continent Continent { get; set; } = new();

    /// <summary>Country-level information.</summary>
    [JsonPropertyName("country")]
    public Country Country { get; set; } = new();

    /// <summary>Administrative region (state/province) information.</summary>
    [JsonPropertyName("region")]
    public Region Region { get; set; } = new();

    /// <summary>The city name.</summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>The postal code.</summary>
    [JsonPropertyName("postal")]
    public string? Postal { get; set; }

    /// <summary>The decimal-degree latitude, or null when unavailable.</summary>
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    /// <summary>The decimal-degree longitude, or null when unavailable.</summary>
    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    /// <summary>The primary language spoken at the location.</summary>
    [JsonPropertyName("language")]
    public Language Language { get; set; } = new();

    /// <summary>Whether the location is within a European Union member state.</summary>
    [JsonPropertyName("in_eu")]
    public bool InEu { get; set; }
}

/// <summary>Holds continent-level information for a location.</summary>
public sealed record Continent
{
    /// <summary>The continent code (for example "NA").</summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>The continent name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>Holds country-level information for a location.</summary>
public sealed record Country
{
    /// <summary>The total land area in square kilometers.</summary>
    [JsonPropertyName("area")]
    public double Area { get; set; }

    /// <summary>The ISO 3166-1 alpha-2 codes of bordering countries.</summary>
    [JsonPropertyName("borders")]
    public IReadOnlyList<string> Borders { get; set; } = [];

    /// <summary>The international calling code (for example "1").</summary>
    [JsonPropertyName("calling_code")]
    public string? CallingCode { get; set; }

    /// <summary>The capital city name.</summary>
    [JsonPropertyName("capital")]
    public string? Capital { get; set; }

    /// <summary>The ISO 3166-1 alpha-2 country code (for example "US").</summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>The country name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The estimated number of inhabitants.</summary>
    [JsonPropertyName("population")]
    public long Population { get; set; }

    /// <summary>The number of inhabitants per square kilometer.</summary>
    [JsonPropertyName("population_density")]
    public double PopulationDensity { get; set; }

    /// <summary>Representations of the country flag across several icon sets.</summary>
    [JsonPropertyName("flag")]
    public Flag Flag { get; set; } = new();

    /// <summary>The languages spoken in the country.</summary>
    [JsonPropertyName("languages")]
    public IReadOnlyList<Language> Languages { get; set; } = [];

    /// <summary>The country-code top-level domain (for example ".us").</summary>
    [JsonPropertyName("tld")]
    public string? Tld { get; set; }
}

/// <summary>Holds administrative region (state/province) information.</summary>
public sealed record Region
{
    /// <summary>Typically the ISO 3166-2 subdivision code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>The region name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>Holds language information.</summary>
public sealed record Language
{
    /// <summary>The language code (for example "en").</summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>The language name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The language's name in the language itself.</summary>
    [JsonPropertyName("native")]
    public string? NativeName { get; set; }
}

/// <summary>Holds representations of a country flag across several icon sets.</summary>
public sealed record Flag
{
    /// <summary>The flag as an emoji character.</summary>
    [JsonPropertyName("emoji")]
    public string? Emoji { get; set; }

    /// <summary>The Unicode code points of the flag emoji.</summary>
    [JsonPropertyName("emoji_unicode")]
    public string? EmojiUnicode { get; set; }

    /// <summary>URL of the EmojiTwo flag icon.</summary>
    [JsonPropertyName("emojitwo")]
    public string? Emojitwo { get; set; }

    /// <summary>URL of the Noto flag icon.</summary>
    [JsonPropertyName("noto")]
    public string? Noto { get; set; }

    /// <summary>URL of the Twemoji flag icon.</summary>
    [JsonPropertyName("twemoji")]
    public string? Twemoji { get; set; }

    /// <summary>URL of the Wikimedia flag image.</summary>
    [JsonPropertyName("wikimedia")]
    public string? Wikimedia { get; set; }
}
