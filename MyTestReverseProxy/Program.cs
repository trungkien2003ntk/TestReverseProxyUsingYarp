using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using MyTestReverseProxy.EntityFrameworkCore;
using Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHttpLogging(o => o = new HttpLoggingOptions());
builder.Services.AddDbContext<ApiIdentityDbContext>(options =>
{
    var configuration = builder.Configuration;
    options.UseSqlServer(configuration.GetConnectionString("AbpIdentity"))
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        .EnableSensitiveDataLogging();
});
builder.Services.AddLogging();
builder.Services.AddMemoryCache(options =>
{
});


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpLogging();

// Enable endpoint routing, required for the reverse proxy.
app.UseRouting();

// Register the reverse proxy routes.
app.MapReverseProxy(proxyPipeline =>
{
    // Use a custom proxy middleware:
    proxyPipeline.Use(CheckApiKey);

    // Don't forget to include these two middleware when you make a custom proxy pipeline (if you need them).
    proxyPipeline.UseSessionAffinity();
    proxyPipeline.UseLoadBalancing();
});

app.Run();

static async Task CheckApiKey(HttpContext context, Func<Task> next)
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    // Check for API Key and Application Name
    if (!TryGetHeader(context, "X-Api-Key", out var apiKeyValues, logger) ||
        !TryGetHeader(context, "X-Application-Name", out var appNameValues, logger))
    {
        return;
    }

    var receivedApiKey = apiKeyValues.ToString();
    var appName = appNameValues.ToString();

    // Retrieve and validate environment variables
    if (!TryGetEncryptionKeys(out var encryptionKey, out var encryptionIV, logger))
    {
        return;
    }

    var encryptedReceivedApiKey = KeyHelper.EncryptString(receivedApiKey, encryptionKey, encryptionIV);

    // Validate API key
    if (!await ValidateApiKey(context, appName, encryptedReceivedApiKey, logger))
    {
        return;
    }

    await next();
}

static bool TryGetHeader(HttpContext context, string headerName, out StringValues headerValues, ILogger logger)
{
    if (!context.Request.Headers.TryGetValue(headerName, out headerValues))
    {
        context.Response.StatusCode = headerName == "X-Api-Key" ? 401 : 400;
        var message = $"{headerName} missing. Please specify the {headerName} header in your request.";
        logger.LogError("{headerName} is missing", headerName);
        context.Response.WriteAsync(message).Wait();
        return false;
    }
    return true;
}

static bool TryGetEncryptionKeys(out string encryptionKey, out string encryptionIV, ILogger logger)
{
    var key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
    var iv = Environment.GetEnvironmentVariable("ENCRYPTION_IV");

    if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv))
    {
        var errorMessage = "Encryption key or IV is not set in the environment variables.";
        logger.LogError(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    encryptionKey = key;
    encryptionIV = iv;

    return true;
}

static async Task<bool> ValidateApiKey(HttpContext context, string appName, byte[] encryptedReceivedApiKey, ILogger logger)
{
    var memCache = context.RequestServices.GetRequiredService<IMemoryCache>();
    var cacheKey = $"{appName}_ApiKey";

    memCache.TryGetValue(cacheKey, out byte[]? encryptedApiKey);

    if (encryptedApiKey == null)
    {
        encryptedApiKey = await GetApiKeyFromDbAsync(context, appName);
    }

    if (encryptedApiKey == null || !encryptedReceivedApiKey.SequenceEqual(encryptedApiKey))
    {
        context.Response.StatusCode = 403;
        var message = "API Key is invalid.";
        logger.LogError(message);
        await context.Response.WriteAsync(message);
        return false;
    }

    // Cache the result of the query
    if (!memCache.TryGetValue(cacheKey, out _))
    {
        memCache.Set(cacheKey, encryptedApiKey, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
        });
    }
    return true;
}

static async Task<byte[]?> GetApiKeyFromDbAsync(HttpContext context, string appName)
{
    using var scope = context.RequestServices.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApiIdentityDbContext>();
    return (await dbContext.ApiKeys
        .FirstOrDefaultAsync(x =>
            x.KeyName == appName &&
            x.IsActive && !x.IsExpired
        ))?.EncyptedValue;
}