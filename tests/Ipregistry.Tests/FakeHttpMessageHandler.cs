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

namespace Ipregistry.Tests;

/// <summary>
/// An HttpMessageHandler test double that records every request and replays a
/// scripted sequence of responses (the last response repeats once the script is
/// exhausted).
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<Func<HttpRequestMessage, HttpResponseMessage>> _responses = [];
    private readonly List<RecordedRequest> _requests = [];
    private int _next;

    public sealed record RecordedRequest(HttpMethod Method, Uri? Uri, string? Body, string? Authorization, string? UserAgent);

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    public FakeHttpMessageHandler Enqueue(HttpStatusCode status, string body, params (string Name, string Value)[] headers)
    {
        _responses.Add(_ =>
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            foreach (var (name, value) in headers)
            {
                response.Headers.TryAddWithoutValidation(name, value);
            }

            return response;
        });
        return this;
    }

    public FakeHttpMessageHandler Enqueue(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responses.Add(responder);
        return this;
    }

    public FakeHttpMessageHandler EnqueueTransportError(Exception exception)
    {
        _responses.Add(_ => throw exception);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? body = null;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        Func<HttpRequestMessage, HttpResponseMessage> responder;
        lock (_requests)
        {
            _requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri,
                body,
                request.Headers.TryGetValues("Authorization", out var auth) ? string.Join(" ", auth) : null,
                request.Headers.TryGetValues("User-Agent", out var userAgent) ? string.Join(" ", userAgent) : null));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No scripted response available.");
            }

            responder = _responses[Math.Min(_next, _responses.Count - 1)];
            _next++;
        }

        return responder(request);
    }
}
