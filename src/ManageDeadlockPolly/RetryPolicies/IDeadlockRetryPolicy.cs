namespace ManageDeadlockPolly.RetryPolicies;

/// <summary>
/// Abstraction pour une stratégie de retry sur deadlock.
/// Découple l'implémentation Polly du reste de l'application.
/// </summary>
public interface IDeadlockRetryPolicy
{
    /// <summary>
    /// Exécute une action avec retry automatique sur deadlock.
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exécute une action sans valeur de retour avec retry automatique.
    /// </summary>
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exécute une action synchrone avec retry (wrapper).
    /// </summary>
    T Execute<T>(Func<T> action);
}
