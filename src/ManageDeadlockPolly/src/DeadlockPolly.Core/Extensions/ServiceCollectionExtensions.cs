using DeadlockPolly.Core.DataAccess;
using DeadlockPolly.Core.RetryPolicies;
using Microsoft.Extensions.DependencyInjection;

namespace DeadlockPolly.Core.Extensions;

/// <summary>
/// Extensions pour enregistrer la pile retry + transactionnel dans le conteneur DI.
///
/// Usage typique :
/// <code>
/// services.AddDeadlockRetryStack(
///     connectionString: "Server=...;",
///     configureRetry: opt => opt.MaxRetries = 5);
/// </code>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Enregistre la stratégie Polly de retry sur deadlock SQL Server.</summary>
    public static IServiceCollection AddDeadlockRetryPolicy(
        this IServiceCollection services,
        Action<DeadlockRetryPolicyOptions>? configure = null)
    {
        var options = new DeadlockRetryPolicyOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton<IDeadlockRetryPolicy>(_ => new PollyDeadlockRetryPolicy(options));
        return services;
    }

    /// <summary>Enregistre le fournisseur de connexion SQL Server.</summary>
    public static IServiceCollection AddSqlServerDataAccess(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string ne peut pas être vide", nameof(connectionString));

        services.AddScoped<IDbConnectionProvider>(_ => new SqlServerConnectionProvider(connectionString));
        return services;
    }

    /// <summary>
    /// Enregistre l'exécuteur transactionnel générique.
    /// Nécessite que <see cref="IDbConnectionProvider"/> et <see cref="IDeadlockRetryPolicy"/>
    /// soient déjà enregistrés.
    /// </summary>
    public static IServiceCollection AddTransactionalExecutor(this IServiceCollection services)
    {
        services.AddScoped<ITransactionalExecutor, TransactionalExecutor>();
        return services;
    }

    /// <summary>Configuration fluide complète : retry + connexion + exécuteur transactionnel.</summary>
    public static IServiceCollection AddDeadlockRetryStack(
        this IServiceCollection services,
        string connectionString,
        Action<DeadlockRetryPolicyOptions>? configureRetry = null)
    {
        return services
            .AddDeadlockRetryPolicy(configureRetry)
            .AddSqlServerDataAccess(connectionString)
            .AddTransactionalExecutor();
    }
}
