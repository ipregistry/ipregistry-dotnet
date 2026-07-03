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

using System.Net;
using System.Text;

namespace Ipregistry;

/// <summary>
/// A strongly typed Ipregistry API error code. It lets callers branch on error
/// conditions without matching on raw strings. See
/// https://ipregistry.co/docs/errors for the authoritative list.
/// </summary>
public enum IpregistryErrorCode
{
    /// <summary>The raw code returned by the API is not recognized by this library version.</summary>
    Unknown = 0,

    /// <summary>The request is malformed.</summary>
    BadRequest,

    /// <summary>The API key has been disabled.</summary>
    DisabledApiKey,

    /// <summary>The IP address is not allowed to use the API key.</summary>
    ForbiddenIp,

    /// <summary>The origin is not allowed to use the API key.</summary>
    ForbiddenOrigin,

    /// <summary>Both the IP address and origin are not allowed to use the API key.</summary>
    ForbiddenIpOrigin,

    /// <summary>An internal error occurred on the Ipregistry side.</summary>
    Internal,

    /// <summary>The account has run out of credits.</summary>
    InsufficientCredits,

    /// <summary>The API key is invalid.</summary>
    InvalidApiKey,

    /// <summary>The Autonomous System Number is invalid.</summary>
    InvalidAsn,

    /// <summary>The field-selection expression has invalid syntax.</summary>
    InvalidFilterSyntax,

    /// <summary>The IP address is invalid.</summary>
    InvalidIpAddress,

    /// <summary>No API key was supplied.</summary>
    MissingApiKey,

    /// <summary>The Autonomous System Number is reserved.</summary>
    ReservedAsn,

    /// <summary>The IP address is reserved (for example a private range).</summary>
    ReservedIpAddress,

    /// <summary>Too many Autonomous System Numbers in one batch request.</summary>
    TooManyAsns,

    /// <summary>Too many IP addresses in one batch request.</summary>
    TooManyIps,

    /// <summary>The API key is being rate limited.</summary>
    TooManyRequests,

    /// <summary>Too many User-Agent strings in one batch request.</summary>
    TooManyUserAgents,

    /// <summary>The Autonomous System Number is unknown.</summary>
    UnknownAsn,
}

/// <summary>Helpers for <see cref="IpregistryErrorCode"/>.</summary>
public static class IpregistryErrorCodes
{
    private static readonly Dictionary<string, IpregistryErrorCode> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BAD_REQUEST"] = IpregistryErrorCode.BadRequest,
        ["DISABLED_API_KEY"] = IpregistryErrorCode.DisabledApiKey,
        ["FORBIDDEN_IP"] = IpregistryErrorCode.ForbiddenIp,
        ["FORBIDDEN_ORIGIN"] = IpregistryErrorCode.ForbiddenOrigin,
        ["FORBIDDEN_IP_ORIGIN"] = IpregistryErrorCode.ForbiddenIpOrigin,
        ["INTERNAL"] = IpregistryErrorCode.Internal,
        ["INSUFFICIENT_CREDITS"] = IpregistryErrorCode.InsufficientCredits,
        ["INVALID_API_KEY"] = IpregistryErrorCode.InvalidApiKey,
        ["INVALID_ASN"] = IpregistryErrorCode.InvalidAsn,
        ["INVALID_FILTER_SYNTAX"] = IpregistryErrorCode.InvalidFilterSyntax,
        ["INVALID_IP_ADDRESS"] = IpregistryErrorCode.InvalidIpAddress,
        ["MISSING_API_KEY"] = IpregistryErrorCode.MissingApiKey,
        ["RESERVED_ASN"] = IpregistryErrorCode.ReservedAsn,
        ["RESERVED_IP_ADDRESS"] = IpregistryErrorCode.ReservedIpAddress,
        ["TOO_MANY_ASNS"] = IpregistryErrorCode.TooManyAsns,
        ["TOO_MANY_IPS"] = IpregistryErrorCode.TooManyIps,
        ["TOO_MANY_REQUESTS"] = IpregistryErrorCode.TooManyRequests,
        ["TOO_MANY_USER_AGENTS"] = IpregistryErrorCode.TooManyUserAgents,
        ["UNKNOWN_ASN"] = IpregistryErrorCode.UnknownAsn,
    };

    /// <summary>
    /// Maps a raw API error code to its typed <see cref="IpregistryErrorCode"/>.
    /// Returns <see cref="IpregistryErrorCode.Unknown"/> when the raw code is not recognized.
    /// </summary>
    /// <param name="rawCode">The raw error code string as returned by the API.</param>
    public static IpregistryErrorCode Parse(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return IpregistryErrorCode.Unknown;
        }

        return Known.TryGetValue(rawCode.Trim(), out var code) ? code : IpregistryErrorCode.Unknown;
    }
}

/// <summary>
/// The base type for all exceptions thrown by this library. Catch it to handle
/// any Ipregistry failure; catch <see cref="IpregistryApiException"/> or
/// <see cref="IpregistryClientException"/> to discriminate API-reported errors
/// from client-side ones.
/// </summary>
public abstract class IpregistryException : Exception
{
    private protected IpregistryException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when the Ipregistry API reports a failure, such as an invalid IP
/// address, an exhausted credit balance, or throttling. It carries both the raw
/// <see cref="Code"/> and, when recognized, the typed <see cref="ErrorCode"/>.
/// </summary>
/// <remarks>
/// In batch lookups, an <see cref="IpregistryApiException"/> may also describe the
/// failure of a single entry rather than the whole request (see <see cref="IpInfoResult"/>
/// and <see cref="UserAgentResult"/>); such per-entry errors are returned, not thrown.
/// </remarks>
public sealed class IpregistryApiException : IpregistryException
{
    /// <summary>Creates an API exception.</summary>
    /// <param name="code">The raw error code returned by the API, or null.</param>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="resolution">A suggestion on how to resolve the error, when available.</param>
    /// <param name="statusCode">The HTTP status code of the response, when known.</param>
    public IpregistryApiException(string? code, string message, string? resolution = null, HttpStatusCode? statusCode = null)
        : base(BuildMessage(code, message, resolution))
    {
        Code = code;
        ErrorCode = IpregistryErrorCodes.Parse(code);
        Resolution = resolution;
        StatusCode = statusCode;
    }

    /// <summary>The raw error code returned by the API, or null when the response carried none.</summary>
    public string? Code { get; }

    /// <summary>The typed form of <see cref="Code"/>, or <see cref="IpregistryErrorCode.Unknown"/> if not recognized.</summary>
    public IpregistryErrorCode ErrorCode { get; }

    /// <summary>A suggestion on how to resolve the error, when available.</summary>
    public string? Resolution { get; }

    /// <summary>The HTTP status code of the response, when known. Per-entry batch errors carry none.</summary>
    public HttpStatusCode? StatusCode { get; }

    private static string BuildMessage(string? code, string message, string? resolution)
    {
        var builder = new StringBuilder("ipregistry: ");
        builder.Append(string.IsNullOrEmpty(message) ? "API error" : message);
        if (!string.IsNullOrEmpty(code))
        {
            builder.Append(" (").Append(code).Append(')');
        }

        if (!string.IsNullOrEmpty(resolution))
        {
            builder.Append(": ").Append(resolution);
        }

        return builder.ToString();
    }
}

/// <summary>
/// Thrown for failures that occur on the client side rather than being reported
/// by the API, such as network errors or a response that cannot be decoded. The
/// underlying cause, when any, is available through <see cref="Exception.InnerException"/>.
/// </summary>
/// <remarks>
/// Cancellation is not wrapped: when the caller's <see cref="CancellationToken"/> is
/// canceled, methods throw <see cref="OperationCanceledException"/> as usual in .NET.
/// </remarks>
public sealed class IpregistryClientException : IpregistryException
{
    /// <summary>Creates a client-side exception.</summary>
    /// <param name="message">A description of the failure.</param>
    /// <param name="innerException">The underlying cause, when any.</param>
    public IpregistryClientException(string message, Exception? innerException = null)
        : base("ipregistry: " + message, innerException)
    {
    }
}
