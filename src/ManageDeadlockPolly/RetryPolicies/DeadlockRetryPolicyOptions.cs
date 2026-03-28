namespace ManageDeadlockPolly.RetryPolicies;

/// <summary>
/// Configuration pour la stratégie de retry sur deadlock.
/// </summary>
public class DeadlockRetryPolicyOptions
{
    /// <summary>
    /// Nombre maximum de tentatives (défaut: 5).
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Délai initial en millisecondes pour le backoff exponentiel (défaut: 100ms).
    /// </summary>
    public int InitialDelayMs { get; set; } = 100;

    /// <summary>
    /// Jitter maximum en millisecondes pour éviter la thundering herd (défaut: 50ms).
    /// </summary>
    public int MaxJitterMs { get; set; } = 50;

    /// <summary>
    /// Code d'erreur SQL Server pour deadlock (defaut: 1205).
    /// </summary>
    public int DeadlockErrorNumber { get; set; } = 1205;

    /// <summary>
    /// Action de callback lors d'un retry (ex: logging).
    /// </summary>
    public Action<int, TimeSpan>? OnRetry { get; set; }

    /// <summary>
    /// Valide les options de configuration.
    /// </summary>
    public void Validate()
    {
        if (MaxRetries <= 0)
            throw new ArgumentException("MaxRetries doit être > 0", nameof(MaxRetries));

        if (InitialDelayMs <= 0)
            throw new ArgumentException("InitialDelayMs doit être > 0", nameof(InitialDelayMs));

        if (MaxJitterMs < 0)
            throw new ArgumentException("MaxJitterMs doit être >= 0", nameof(MaxJitterMs));
    }
}
