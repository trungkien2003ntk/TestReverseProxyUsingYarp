using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ApiKeyGenerator.EntityFrameworkCore;

internal class ApiKeyGeneratorDbContextFactory : IDesignTimeDbContextFactory<ApiKeyGeneratorDbContext>
{
    public ApiKeyGeneratorDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<ApiKeyGeneratorDbContext>()
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

        return new ApiKeyGeneratorDbContext(builder.Options, configuration);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}

