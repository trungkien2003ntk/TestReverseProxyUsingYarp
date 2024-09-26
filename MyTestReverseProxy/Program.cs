using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHttpLogging(o => o = new HttpLoggingOptions());

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
    // Check for API Key
    if (!context.Request.Headers.ContainsKey("X-Api-Key"))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("API Key missing.");
        return;
    }

    // Get the token (for example, from a header or query string)
    var apiKey = context.Request.Headers["X-Api-Key"].ToString();

    // If the API Key is valid (for this example, we use '12345')
    if (apiKey == "12345")
    {
        //// Inject the token (without the "Bearer" prefix) into the request header
        //context.Request.Headers["Authorization"] = apiKey;
        Console.WriteLine($"API Key: {apiKey}");
    }
    else
    {
        context.Response.StatusCode = 403; // Forbidden if the API key is invalid
        return;
    }

    await next();
}
