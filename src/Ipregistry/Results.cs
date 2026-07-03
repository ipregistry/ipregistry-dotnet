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

using System.Diagnostics.CodeAnalysis;

namespace Ipregistry;

/// <summary>
/// One entry of a batch IP lookup. Exactly one of <see cref="Info"/> and
/// <see cref="Error"/> is non-null: <see cref="Info"/> holds the data for a
/// successfully resolved IP address, and <see cref="Error"/> describes why that
/// particular entry failed.
/// </summary>
public sealed class IpInfoResult
{
    /// <summary>Creates a successful entry.</summary>
    public IpInfoResult(IpInfo info)
    {
        Info = info ?? throw new ArgumentNullException(nameof(info));
    }

    /// <summary>Creates a failed entry.</summary>
    public IpInfoResult(IpregistryApiException error)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>The resolved data, or null when the entry failed.</summary>
    public IpInfo? Info { get; }

    /// <summary>The failure of this entry, or null when it succeeded. It is returned, never thrown.</summary>
    public IpregistryApiException? Error { get; }

    /// <summary>Whether the entry resolved successfully.</summary>
    [MemberNotNullWhen(true, nameof(Info))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Info is not null;

    /// <summary>The resolved data; throws <see cref="Error"/> when the entry failed.</summary>
    public IpInfo Value => Info ?? throw Error!;
}

/// <summary>
/// One entry of a batch User-Agent parse. Exactly one of <see cref="UserAgent"/>
/// and <see cref="Error"/> is non-null.
/// </summary>
public sealed class UserAgentResult
{
    /// <summary>Creates a successful entry.</summary>
    public UserAgentResult(UserAgent userAgent)
    {
        UserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
    }

    /// <summary>Creates a failed entry.</summary>
    public UserAgentResult(IpregistryApiException error)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>The parsed User-Agent, or null when the entry failed.</summary>
    public UserAgent? UserAgent { get; }

    /// <summary>The failure of this entry, or null when it succeeded. It is returned, never thrown.</summary>
    public IpregistryApiException? Error { get; }

    /// <summary>Whether the entry parsed successfully.</summary>
    [MemberNotNullWhen(true, nameof(UserAgent))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => UserAgent is not null;

    /// <summary>The parsed User-Agent; throws <see cref="Error"/> when the entry failed.</summary>
    public UserAgent Value => UserAgent ?? throw Error!;
}
