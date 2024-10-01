using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace MyTestReverseProxy.EntityFrameworkCore;

public class ApiIdentityDbContext : DbContext
{
    public ApiIdentityDbContext(DbContextOptions<ApiIdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApiKey>(b =>
        {
            b.ToTable("AppApiKey");
            b.HasKey(e => e.Id);
            b.Property(e => e.KeyName).HasColumnName(nameof(ApiKey.KeyName)).IsRequired();
            b.Property(e => e.EncyptedValue).HasColumnName(nameof(ApiKey.EncyptedValue)).IsRequired().HasColumnType("varbinary(max)");
            b.Property(e => e.IsActive).HasColumnName(nameof(ApiKey.IsActive)).IsRequired();
            b.Property(e => e.DeactiveDate).HasColumnName(nameof(ApiKey.DeactiveDate));
            b.Property(e => e.IsExpired).HasColumnName(nameof(ApiKey.IsExpired)).IsRequired();
            b.Property(e => e.Expires).HasColumnName(nameof(ApiKey.Expires));
            b.Property(e => e.Notes).HasColumnName(nameof(ApiKey.Notes));
            b.ConfigureByConvention();
        });
    }

    public DbSet<ApiKey> ApiKeys { get; set; } = null!;
}
