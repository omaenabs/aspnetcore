// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal class LoggingHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHttpMessageHandler> _logger;

    public LoggingHttpMessageHandler(HttpMessageHandler inner, ILoggerFactory loggerFactory) : base(inner)
    {
        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        _logger = loggerFactory.CreateLogger<LoggingHttpMessageHandler>();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Log.SendingHttpRequest(_logger, request.Method, request.RequestUri!);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            Log.UnsuccessfulHttpResponse(_logger, response.StatusCode, request.Method, request.RequestUri!);
        }

        return response;
    }

    private static class Log
    {
        private static readonly Action<ILogger, HttpMethod, Uri, Exception?> _sendingHttpRequest =
            LoggerMessage.Define<HttpMethod, Uri>(LogLevel.Trace, new EventId(1, "SendingHttpRequest"), "Sending HTTP request {RequestMethod} '{RequestUrl}'.");

        private static readonly Action<ILogger, int, HttpMethod, Uri, Exception?> _unsuccessfulHttpResponse =
            LoggerMessage.Define<int, HttpMethod, Uri>(LogLevel.Warning, new EventId(2, "UnsuccessfulHttpResponse"), "Unsuccessful HTTP response {StatusCode} return from {RequestMethod} '{RequestUrl}'.");

        public static void SendingHttpRequest(ILogger logger, HttpMethod requestMethod, Uri requestUrl)
        {
            _sendingHttpRequest(logger, requestMethod, requestUrl, null);
        }
        public static void UnsuccessfulHttpResponse(ILogger logger, HttpStatusCode statusCode, HttpMethod requestMethod, Uri requestUrl)
        {
            _unsuccessfulHttpResponse(logger, (int)statusCode, requestMethod, requestUrl, null);
        }
    }
}
