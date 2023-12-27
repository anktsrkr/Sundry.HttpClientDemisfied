
using System;
using System.Net.Http;

namespace Sundry.HttpClientDemisfied.CustomLogger;

using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;

public class TestILoggerCustomLogger : IHttpClientLogger
{
    private readonly ILogger _logger;
    public TestILoggerCustomLogger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TestILoggerCustomLogger>();
    }

    public object? LogRequestStart(HttpRequestMessage request)
    {
        _logger.LogInformation("LogRequestStart");
        return null;
    }

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
        => _logger.LogInformation("LogRequestStop");

    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
        => _logger.LogInformation("LogRequestFailed");
}
