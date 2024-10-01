namespace ApiKeyGenerator;

internal static class Extensions
{
    public static bool IsAvaiable(this ApiKey key)
    {
        return key.IsActive && !key.IsExpired && (key.Expires == null || key.Expires > DateTime.Now) && (key.DeactiveDate == null || key.DeactiveDate > DateTime.Now);
    }
}

