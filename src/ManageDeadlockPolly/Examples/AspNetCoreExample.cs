// AspNetCore.Example.cs
// Exemple d'intégration dans une app ASP.NET Core avec DI et Logging

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManageDeadlockPolly.Examples;

/// <summary>
/// Exemple complet d'intégration dans ASP.NET Core.
/// </summary>
public static class AspNetCoreIntegration
{
    /// <summary>
    /// Enregistrer les services Deadlock Retry dans le conteneur DI.
    /// Ajouter ceci dans Program.cs:
    ///   builder.Services.AddDeadlockRetryServices(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddDeadlockRetryServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found");

        // Enregistrer le service singleton
        services.AddSingleton<DeadlockRetryService>(sp =>
            new DeadlockRetryService(
                connectionString,
                sp.GetRequiredService<ILogger<DeadlockRetryService>>(),
                maxRetries: 5
            )
        );

        // Enregistrer les repositories
        services.AddScoped<DeadlockTestRepository>();

        return services;
    }
}

/// <summary>
/// Exemple d'API Controller utilisant Polly + Dapper.
/// </summary>
/// <example>
/// GET /api/deadlock-test → Retourne l'état actuel
/// POST /api/deadlock-test/increment → Incrémente les deux valeurs
/// POST /api/deadlock-test/reset → Reset à 0
/// </example>
public class DeadlockTestController
{
    private readonly DeadlockTestRepository _repository;
    private readonly ILogger<DeadlockTestController> _logger;

    public DeadlockTestController(
        DeadlockTestRepository repository,
        ILogger<DeadlockTestController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/deadlock-test
    /// Retourne l'état actuel de la table.
    /// </summary>
    [HttpGet("/api/deadlock-test")]
    public async Task<IActionResult> GetValues()
    {
        try
        {
            var values = await _repository.GetValuesAsync();
            return Ok(new { success = true, data = values });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des valeurs");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/deadlock-test/increment
    /// Incrémente les deux valeurs avec retry automatique sur deadlock.
    /// </summary>
    [HttpPost("/api/deadlock-test/increment")]
    public async Task<IActionResult> Increment()
    {
        try
        {
            var updated = await _repository.IncrementBothValuesAsync();
            _logger.LogInformation($"Increment réussi: {updated} enregistrements mis à jour");

            var values = await _repository.GetValuesAsync();
            return Ok(new { success = true, updated, data = values });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du increment");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/deadlock-test/reset
    /// Reset les valeurs à 0.
    /// </summary>
    [HttpPost("/api/deadlock-test/reset")]
    public async Task<IActionResult> Reset()
    {
        try
        {
            await _repository.ResetAsync();
            _logger.LogInformation("Reset effectué");
            return Ok(new { success = true, message = "Reset terminé" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du reset");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

/// <summary>
/// Exemple Program.cs pour ASP.NET Core.
/// </summary>
public class ProgramExample
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ========== Configuration ==========

        // 1. Logging avec Serilog (optionnel mais recommandé)
        builder.Services.AddLogging(config =>
        {
            config.ClearProviders();
            config.AddConsole();
            config.AddDebug();
            config.SetMinimumLevel(LogLevel.Information);
            // + Serilog si installé
            // config.AddSerilog(...);
        });

        // 2. Controllers
        builder.Services.AddControllers();

        // 3. Ajouter Deadlock Retry Service
        builder.Services.AddDeadlockRetryServices(builder.Configuration);

        // 4. Swagger/OpenAPI (optionnel)
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // 5. CORS (si API distante)
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // ========== Build & Run ==========

        var app = builder.Build();

        // Middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors();
        app.MapControllers();

        app.Run();
    }
}

/// <summary>
/// Exemple appsettings.json
/// </summary>
public class AppSettingsExample
{
    public static string ExampleJson => """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft": "Warning",
              "ManageDeadlockPolly": "Debug"
            }
          },
          "ConnectionStrings": {
            "DefaultConnection": "Server=sql-server,1433;Database=DeadlockTestDb;User Id=sa;Password=YourStrong!Pass2024;Encrypt=false;TrustServerCertificate=true;"
          },
          "Polly": {
            "DeadlockRetry": {
              "MaxRetryAttempts": 5,
              "InitialDelayMs": 100,
              "BackoffMultiplier": 2.0,
              "MaxJitterMs": 50
            }
          },
          "AllowedHosts": "*"
        }
        """;
}

/// <summary>
/// Exemple de test unitaire pour le service.
/// </summary>
public class DeadlockRetryServiceTests
{
    [Fact]
    public async Task ExecuteWithDeadlockRetry_OnSuccess_ReturnsResult()
    {
        // Arrange
        var service = new DeadlockRetryService("Server=localhost;...", maxRetries: 3);

        // Act
        var result = await service.ExecuteWithDeadlockRetryAsync(async (conn, tx) =>
        {
            return "success";
        });

        // Assert
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task ExecuteWithDeadlockRetry_OnDeadlock_RetriesAndSucceeds()
    {
        // Arrange
        var service = new DeadlockRetryService("Server=localhost;...", maxRetries: 5);
        var attemptCount = 0;

        // Act
        var result = await service.ExecuteWithDeadlockRetryAsync(async (conn, tx) =>
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new SqlException("Deadlock", new Exception() { HResult = 1205 });

            return "success after retries";
        });

        // Assert
        Assert.Equal("success after retries", result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithDeadlockRetry_MaxRetriesExceeded_Throws()
    {
        // Arrange
        var service = new DeadlockRetryService("Server=localhost;...", maxRetries: 2);

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await service.ExecuteWithDeadlockRetryAsync(async (conn, tx) =>
            {
                throw new SqlException("Always deadlock");
            });
        });
    }
}

/// <summary>
/// Exemple de test d'intégration pour l'API.
/// </summary>
public class DeadlockTestControllerIntegrationTests
{
    [Fact]
    public async Task Increment_WithConcurrentRequests_AllSucceed()
    {
        // Arrange
        // Utiliser TestServer ou WebApplicationFactory
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddDeadlockRetryServices(builder.Configuration);
        builder.Services.AddControllers();
        var app = builder.Build();
        app.MapControllers();

        using var client = new HttpClient { BaseAddress = new Uri("http://localhost") };

        // Act - Lancer plusieurs requêtes concurrentes
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => client.PostAsync("/api/deadlock-test/increment", null))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.True(r.IsSuccessStatusCode));
    }
}

/// <summary>
/// Exemple de middleware pour capturer les deadlocks.
/// </summary>
public class DeadlockDialgnosticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeadlockDialgnosticsMiddleware> _logger;

    public DeadlockDialgnosticsMiddleware(RequestDelegate next, ILogger<DeadlockDialgnosticsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            _logger.LogWarning(
                "Deadlock détecté sur {Path} {Method}. Polly devrait retry.",
                context.Request.Path,
                context.Request.Method
            );
            throw;
        }
    }
}

// Extension pour enregistrer le middleware
public static class DeadlockDiagnosticsExtensions
{
    public static IApplicationBuilder UseDeadlockDiagnostics(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DeadlockDialgnosticsMiddleware>();
    }
}

/// <summary>
/// Utilisation dans Program.cs:
/// 
/// var app = builder.Build();
/// app.UseDeadlockDiagnostics();
/// app.MapControllers();
/// app.Run();
/// </summary>
/// 