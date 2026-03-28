using System.Data;
using Dapper;
using ManageDeadlockPolly.DataAccess;
using ManageDeadlockPolly.Extensions;
using ManageDeadlockPolly.RetryPolicies;
using Microsoft.Extensions.DependencyInjection;

namespace ManageDeadlockPolly.Examples;

/// <summary>
/// Exemple d'intégration simple de la pile retry deadlock.
/// Montre comment configurer et utiliser ITransactionalExecutor en mode standalone.
/// 
/// Cet exemple peut être adapté pour:
/// - Une API REST (ASP.NET Core)
/// - Un background worker
/// - Une application console
/// - Un microservice
/// </summary>
public class SimpleIntegrationExample
{
    /// <summary>
    /// Point d'entrée de l'exemple.
    /// </summary>
    public static async Task Main()
    {
        Console.WriteLine("=== Simple Integration Example ===\n");

        // 1. Configuration du conteneur DI
        var services = new ServiceCollection();
        ConfigureServices(services);

        // 2. Obtenir l'executor du conteneur
        var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetRequiredService<ITransactionalExecutor>();
        var connectionString = "Server=localhost,1433;Database=DeadlockTestDb;User Id=sa;Password=YourStrong!Pass2024;Encrypt=false;TrustServerCertificate=true;";

        // 3. Utiliser l'executor dans un repository
        var repository = new ExampleProductRepository(executor);

        // 4. Exécuter des opérations
        try
        {
            await repository.UpdateProductPriceAsync(1, 99.99m);
            Console.WriteLine("✓ Product updated successfully");

            var product = await repository.GetProductAsync(1);
            Console.WriteLine($"✓ Product: Id={product["Id"]}, Price={product["Price"]}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Configure les services DI.
    /// </summary>
    private static void ConfigureServices(ServiceCollection services)
    {
        var connectionString = "Server=localhost,1433;Database=DeadlockTestDb;User Id=sa;Password=YourStrong!Pass2024;Encrypt=false;TrustServerCertificate=true;";

        // Configuration du callback OnRetry pour logging
        Action<DeadlockRetryPolicyOptions> configureRetry = options =>
        {
            options.MaxRetries = 5;
            options.InitialDelayMs = 100;
            options.MaxJitterMs = 50;
            options.OnRetry = (retryCount, delay) =>
            {
                Console.WriteLine($"[RETRY {retryCount}] Deadlock detected, retrying in {delay.TotalMilliseconds:F0}ms");
            };
        };

        // Enregistrement fluide de toute la pile
        services.AddDeadlockRetryStack(connectionString, configureRetry);
    }
}

/// <summary>
/// Repository exemple montrant l'utilisation de ITransactionalExecutor.
/// Ce pattern peut être appliqué à n'importe quel domaine métier.
/// </summary>
public class ExampleProductRepository
{
    private readonly ITransactionalExecutor _executor;

    public ExampleProductRepository(ITransactionalExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Exemple 1: Mettre à jour un produit (avec transaction).
    /// La transaction est automatiquement recréée en cas de deadlock.
    /// </summary>
    public async Task UpdateProductPriceAsync(int productId, decimal newPrice)
    {
        await _executor.ExecuteAsync(async (conn, tx) =>
        {
            var sql = "UPDATE Products SET Price = @Price WHERE Id = @Id";
            await conn.ExecuteAsync(sql, new { Price = newPrice, Id = productId }, transaction: tx);
        });
    }

    /// <summary>
    /// Exemple 2: Lire des données (pas obligatoire de transactionner, mais possible).
    /// </summary>
    public async Task<Dictionary<string, object>> GetProductAsync(int productId)
    {
        return await _executor.ExecuteAsync(async (conn, tx) =>
        {
            var sql = "SELECT Id, Name, Price FROM Products WHERE Id = @Id";
            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = productId }, transaction: tx);

            if (result == null)
                throw new KeyNotFoundException($"Product {productId} not found");

            return new Dictionary<string, object>
            {
                { "Id", result.Id },
                { "Name", result.Name },
                { "Price", result.Price }
            };
        });
    }

    /// <summary>
    /// Exemple 3: Opération complexe avec multiples updates.
    /// Chaque retry redémarre la transaction depuis le début.
    /// </summary>
    public async Task TransferStockAsync(int sourceProductId, int destProductId, int quantity)
    {
        await _executor.ExecuteAsync(async (conn, tx) =>
        {
            // Réduire le stock source
            await conn.ExecuteAsync(
                "UPDATE Products SET Stock = Stock - @Qty WHERE Id = @Id",
                new { Qty = quantity, Id = sourceProductId },
                transaction: tx);

            // Simuler un délai pouvant causer un deadlock
            await Task.Delay(100);

            // Augmenter le stock destination
            await conn.ExecuteAsync(
                "UPDATE Products SET Stock = Stock + @Qty WHERE Id = @Id",
                new { Qty = quantity, Id = destProductId },
                transaction: tx);
        }, IsolationLevel.Serializable);  // Isolation level personnalisé
    }

    /// <summary>
    /// Exemple 4: Opération avec valeur de retour.
    /// </summary>
    public async Task<int> CreateProductAsync(string name, decimal price)
    {
        return await _executor.ExecuteAsync(async (conn, tx) =>
        {
            var sql = "INSERT INTO Products (Name, Price) VALUES (@Name, @Price); SELECT SCOPE_IDENTITY();";
            var id = await conn.ExecuteScalarAsync<int>(sql, new { Name = name, Price = price }, transaction: tx);
            return id;
        });
    }
}

/// <summary>
/// Exemple d'intégration avec un ASP.NET Core project.
/// À utiliser pour copier dans un vrai projet.
/// </summary>
public class AspNetCoreIntegrationExample
{
    // Dans Program.cs:
    // var builder = WebApplication.CreateBuilder(args);
    // var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException();
    // builder.Services.AddDeadlockRetryStack(connectionString, options => options.MaxRetries = 5);
    // builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    // var app = builder.Build();
    // app.MapControllers();
    // app.Run();

    // Dans un controller:
    // [ApiController]
    // public class OrdersController : ControllerBase
    // {
    //     private readonly IOrderRepository _repository;
    //     public OrdersController(IOrderRepository repository) => _repository = repository;
    //
    //     [HttpPost]
    //     public async Task<ActionResult<int>> CreateOrder([FromBody] CreateOrderDto dto)
    //     {
    //         var orderId = await _repository.CreateOrderAsync(dto);
    //         return CreatedAtAction(nameof(CreateOrder), new { id = orderId }, orderId);
    //     }
    // }

    // Dans un repository:
    // public class OrderRepository : IOrderRepository
    // {
    //     private readonly ITransactionalExecutor _executor;
    //     public OrderRepository(ITransactionalExecutor executor) => _executor = executor;
    //
    //     public async Task<int> CreateOrderAsync(CreateOrderDto dto)
    //     {
    //         return await _executor.ExecuteAsync(async (conn, tx) =>
    //         {
    //             var orderId = await conn.ExecuteScalarAsync<int>(
    //                 "INSERT INTO Orders (CustomerId, Total) VALUES (@CustomerId, @Total); SELECT SCOPE_IDENTITY();",
    //                 new { dto.CustomerId, dto.Total },
    //                 transaction: tx);
    //             return orderId;
    //         });
    //     }
    // }
}
