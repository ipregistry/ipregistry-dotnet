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

/// <summary>Holds the structured data parsed from a raw User-Agent string.</summary>
public sealed record UserAgent
{
    /// <summary>The raw User-Agent string that was parsed.</summary>
    [JsonPropertyName("header")]
    public string? Header { get; set; }

    /// <summary>The agent name (for example "Chrome").</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The agent type (for example "browser").</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>The agent version.</summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>The agent major version.</summary>
    [JsonPropertyName("version_major")]
    public string? VersionMajor { get; set; }

    /// <summary>The device data parsed from the User-Agent string.</summary>
    [JsonPropertyName("device")]
    public UserAgentDevice Device { get; set; } = new();

    /// <summary>The layout-engine data parsed from the User-Agent string.</summary>
    [JsonPropertyName("engine")]
    public UserAgentEngine Engine { get; set; } = new();

    /// <summary>The operating-system data parsed from the User-Agent string.</summary>
    [JsonPropertyName("os")]
    public UserAgentOperatingSystem OperatingSystem { get; set; } = new();
}

/// <summary>Holds the device data parsed from a User-Agent string.</summary>
public sealed record UserAgentDevice
{
    /// <summary>The device brand.</summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    /// <summary>The device name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The device type.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>Holds the layout-engine data parsed from a User-Agent string.</summary>
public sealed record UserAgentEngine
{
    /// <summary>The engine name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The engine type.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>The engine version.</summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>The engine major version.</summary>
    [JsonPropertyName("version_major")]
    public string? VersionMajor { get; set; }
}

/// <summary>Holds the operating-system data parsed from a User-Agent string.</summary>
public sealed record UserAgentOperatingSystem
{
    /// <summary>The operating system name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The operating system type.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>The operating system version.</summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

/// <summary>Helpers for working with raw User-Agent strings.</summary>
public static class UserAgents
{
    /// <summary>
    /// Reports whether the given raw User-Agent string looks like a crawler or bot.
    /// It is a lightweight heuristic — useful for skipping IP lookups on automated
    /// traffic — that matches the substrings "bot", "spider", and "slurp"
    /// case-insensitively.
    /// </summary>
    /// <param name="userAgent">The raw User-Agent string to inspect.</param>
    public static bool IsBot(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return false;
        }

        return userAgent.Contains("bot", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("spider", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("slurp", StringComparison.OrdinalIgnoreCase);
    }
}
