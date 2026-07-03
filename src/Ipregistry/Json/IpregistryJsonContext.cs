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

namespace Ipregistry.Json;

/// <summary>
/// Source-generated System.Text.Json context for all payloads exchanged with the
/// Ipregistry API, keeping the library trimming- and AOT-compatible.
/// </summary>
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(IpInfo))]
[JsonSerializable(typeof(RequesterIpInfo))]
[JsonSerializable(typeof(UserAgent))]
[JsonSerializable(typeof(ApiErrorPayload))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class IpregistryJsonContext : JsonSerializerContext
{
}

/// <summary>Mirrors the JSON error body returned by the API.</summary>
internal sealed record ApiErrorPayload
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }
}
