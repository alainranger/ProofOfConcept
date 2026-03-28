using ManageDeadlockPolly.DataAccess;
using ManageDeadlockPolly.RetryPolicies;
using Microsoft.Extensions.DependencyInjection;

namespace ManageDeadlockPolly.Extensions;

/// <summary>
/// Extensions pour enregistrer les services de retry et transactionnel dans le conteneur DI.
/// Utilisation typique:
/// 
/// services
///     .AddDeadlockRetryPolicy(options => options.MaxRetries = 5)
///     .AddSqlServerDataAccess("Server=localhost;...")
///     .AddTransactionalExecutor();
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre la stratégie Polly de retry sur deadlock SQL Server.
    /// </summary>
    public static IServiceCollection AddDeadlockRetryPolicy(
        this IServiceCollection services,
        Action<DeadlockRetryPolicyOptions>? configure = null)
    {
        var options = new DeadlockRetryPolicyOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton<IDeadlockRetryPolicy>(
            _ => new PollyDeadlockRetryPolicy(options));

        return services;
    }

    /// <summary>
    /// Enregistre le fournisseur de connexion SQL Server.
    /// </summary>
    public static IServiceCollection AddSqlServerDataAccess(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string ne peut pas être vide", nameof(connectionString));

        services.AddScoped<IDbConnectionProvider>(
            _ => new SqlServerConnectionProvider(connectionString));

        return services;
    }

    /// <summary>
    /// Enregistre l'exécuteur transactionnel générique.
    /// Nécessite que IDbConnectionProvider et IDeadlockRetryPolicy soient déjà enregistrés.
    /// </summary>
    public static IServiceCollection AddTransactionalExecutor(
        this IServiceCollection services)
    {
        services.AddScoped<ITransactionalExecutor, TransactionalExecutor>();
        return services;
    }

    /// <summary>
    /// Configuration fluide complète de la pile retry + transactionnel.
    /// </summary>
    public static IServiceCollection AddDeadlockRetryStack(
        this IServiceCollection services,
        string connectionString,
        Action<DeadlockRetryPolicyOptions>? configureRetry = null)
    {
        services
            .AddDeadlockRetryPolicy(configureRetry)
            .AddSqlServerDataAccess(connectionString)
            .AddTransactionalExecutor();

        return services;
    }
}
