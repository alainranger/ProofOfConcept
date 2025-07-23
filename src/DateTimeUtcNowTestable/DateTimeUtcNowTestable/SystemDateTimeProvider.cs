namespace DateTimeUtcNowTestable;

// Implémentation système
public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}