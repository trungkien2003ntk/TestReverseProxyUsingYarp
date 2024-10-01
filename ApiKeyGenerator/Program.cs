using ApiKeyGenerator;
using ApiKeyGenerator.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Utilities;

#region Encryption Key Generation

//var encryptionKey = KeyHelper.GenerateApiKey(16);
//Console.WriteLine($"Encryption Key: {encryptionKey}");

//var encryptionIV = KeyHelper.GenerateApiKey(16);
//Console.WriteLine($"Encryption IV: {encryptionIV}");

#endregion


var host = CreateHostBuilder(args).Build();


var generatedApiKey = KeyHelper.GenerateApiKey();
Console.WriteLine("Generated API Key: " + generatedApiKey);
Console.WriteLine("Please store this key in a secure location. This key will not be displayed again.");

// store the key to db
IOptions<EncryptionKey> encryptionKeyOptions = host.Services.GetRequiredService<IOptions<EncryptionKey>>();
using var scope = host.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ApiKeyGeneratorDbContext>();
var keyName = "TestKey";
var existedKeys = await dbContext.ApiKeys
    .Where(key => key.KeyName == keyName &&
        key.IsActive && (key.DeactiveDate == null || key.DeactiveDate > DateTime.Now) &&
        !key.IsExpired && (key.Expires == null || key.Expires > DateTime.Now))
    .ToListAsync();

foreach (var key in existedKeys)
{
    key.IsActive = false;
    key.DeactiveDate = DateTime.Now;
    key.IsExpired = true;
    key.Expires = DateTime.Now;
}

dbContext.UpdateRange(existedKeys);

var apiKey = new ApiKey
{
    KeyName = keyName,
    EncyptedValue = KeyHelper.EncryptString(generatedApiKey, encryptionKeyOptions.Value.Key, encryptionKeyOptions.Value.IV),
    IsActive = true,
    DeactiveDate = null,
    IsExpired = false,
    Expires = DateTime.Now.AddYears(1),
    Notes = "This is a test key."
};

await dbContext.ApiKeys.AddAsync(apiKey);
await dbContext.SaveChangesAsync();
Console.WriteLine("API Key stored in the database.");
Console.ReadLine();

await host.RunAsync();


static HostBuilder CreateHostBuilder(string[] args)
{
    var builder = new HostBuilder();
    builder.ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false);
        config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true);
        config.AddEnvironmentVariables();
        config.AddCommandLine(args);
    });

    builder.ConfigureServices((hostContext, services) =>
    {
        services.AddDbContext<ApiKeyGeneratorDbContext>(options =>
        {
            options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection"))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableSensitiveDataLogging();
        });

        services.Configure<EncryptionKey>(hostContext.Configuration.GetSection(nameof(EncryptionKey)));
    });

    return builder;
}