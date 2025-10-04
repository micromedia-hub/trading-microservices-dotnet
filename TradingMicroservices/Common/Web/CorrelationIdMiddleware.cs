using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;


namespace TradingMicroservices.Common.Web
{
    public static class CorrelationIdMiddleware
    {
        /// <summary>
        /// Ensures every request has X-Correlation-Id; exposes it in response headers and LogContext/ILogger scope.
        /// </summary>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.Use(async (httpContext, next) =>
            {
                string headerName = TradingMicroservices.Common.Constants.Messaging.Headers.CorrelationId;
                var correlationId = httpContext.Request.Headers[headerName].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString("n");
                }
                var userRef = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.Request.Headers[TradingMicroservices.Common.Constants.Messaging.Headers.UserRef].FirstOrDefault()
                    ?? "(anonymous)";
                // Store for later (YARP transform or controllers)
                httpContext.Items[headerName] = correlationId;
                // Write back to response
                httpContext.Response.OnStarting(() =>
                {
                    httpContext.Response.Headers[headerName] = correlationId;
                    return Task.CompletedTask;
                });
                // Structured logging enrichment
                // Serilog
                using (LogContext.PushProperty(TradingMicroservices.Common.Constants.Messaging.LogProperties.CorrelationId, correlationId))
                using (LogContext.PushProperty("UserRef", userRef))
                // Microsoft.Extensions.Logging
                using (httpContext.RequestServices
                          .GetRequiredService<ILoggerFactory>()
                          .CreateLogger("Correlation")
                          .BeginScope(new Dictionary<string, object>
                          {
                              [TradingMicroservices.Common.Constants.Messaging.LogProperties.CorrelationId] = correlationId,
                              [TradingMicroservices.Common.Constants.Messaging.LogProperties.UserRef] = userRef
                          }))
                {
                    await next();
                }
            });
        }
    }
}
