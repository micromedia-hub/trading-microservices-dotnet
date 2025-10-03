using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Yarp.ReverseProxy;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// JWT
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiUser", p => p.RequireAuthenticatedUser());
});

// Rate limiting - simple
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", configure =>
    {
        configure.PermitLimit = 10;
        configure.Window = TimeSpan.FromMinutes(1);
        configure.QueueLimit = 0;
    });
    options.OnRejected = (context, token) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        context.HttpContext.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new
        {
            error = "rate_limited",
            message = "Too many requests. Please try again later."
        });
        return new ValueTask(context.HttpContext.Response.WriteAsync(payload, token));
    };
});

// YARP from config. Transform: forward sub -> X-User-Ref
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(context =>
    {
        context.AddRequestTransform(async transformContext =>
        {
            var sub = transformContext.HttpContext.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(sub))
            {
                transformContext.ProxyRequest.Headers.Remove(TradingMicroservices.Common.Constants.Messaging.Headers.UserRef);
                transformContext.ProxyRequest.Headers.Add(TradingMicroservices.Common.Constants.Messaging.Headers.UserRef, sub);
            }
        });
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Test endpoint to prove Gateway validates JWT
app.MapGet("/dev/whoami", (ClaimsPrincipal user) =>
{
    var sub = user.FindFirst("sub")?.Value ?? "(none)";
    return Results.Ok(new { sub, at = DateTimeOffset.UtcNow });
}).RequireAuthorization("ApiUser").RequireRateLimiting("api");

app.MapReverseProxy().RequireAuthorization("ApiUser").RequireRateLimiting("api");

app.Run();
