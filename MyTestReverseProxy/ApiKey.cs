using Abp.Domain.Entities.Auditing;

namespace MyTestReverseProxy;

public class ApiKey : AuditedAggregateRoot<Guid>
{
    public string KeyName { get; set; } = null!;

    public byte[] EncyptedValue { get; set; } = null!;

    public DateTime? Expires { get; set; }

    public bool IsExpired { get; set; }

    public bool IsActive { get; set; }

    public DateTime? DeactiveDate { get; set; }

    public string? Notes { get; set; }
}
