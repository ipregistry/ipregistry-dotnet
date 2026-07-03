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

using System.Text;

namespace Ipregistry;

/// <summary>
/// Customizes a single lookup or batch request by setting query parameters.
/// </summary>
public sealed class LookupOptions
{
    /// <summary>
    /// Restricts the response to the given fields, using Ipregistry's field
    /// selector syntax (for example "location.country.name,security"). This
    /// reduces payload size and, in some cases, credit usage. See
    /// https://ipregistry.co/docs/filtering-selecting-fields for the syntax.
    /// </summary>
    public string? Fields { get; set; }

    /// <summary>
    /// Enables or disables reverse-DNS hostname resolution for the looked-up IP
    /// addresses. It is disabled by default (null omits the parameter).
    /// </summary>
    public bool? Hostname { get; set; }

    /// <summary>
    /// Arbitrary additional query parameters, for options not covered by a
    /// dedicated property.
    /// </summary>
    public IDictionary<string, string> AdditionalParameters { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Collapses the options into a canonical query string (parameters sorted by
    /// name, values escaped), without a leading '?'. Returns an empty string when
    /// no parameter is set. The canonical form makes cache keys stable regardless
    /// of how the options were populated.
    /// </summary>
    internal string ToQueryString()
    {
        SortedDictionary<string, string> parameters = new(StringComparer.Ordinal);
        foreach (var (name, value) in AdditionalParameters)
        {
            parameters[name] = value;
        }

        if (Fields is not null)
        {
            parameters["fields"] = Fields;
        }

        if (Hostname is not null)
        {
            parameters["hostname"] = Hostname.Value ? "true" : "false";
        }

        if (parameters.Count == 0)
        {
            return "";
        }

        var builder = new StringBuilder();
        foreach (var (name, value) in parameters)
        {
            if (builder.Length > 0)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(name)).Append('=').Append(Uri.EscapeDataString(value));
        }

        return builder.ToString();
    }
}
