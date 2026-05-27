using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [T{ThreadId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new CompactJsonFormatter(), "logs/myapp.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Demarrage de l'application multi-thread");

    var workers = Enumerable.Range(1, 4)
        .Select(workerId => Task.Run(() => DoWorkAsync(workerId)));

    await Task.WhenAll(workers);

    Log.Information("Tous les workers ont termine");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erreur fatale dans l'application");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static async Task DoWorkAsync(int workerId)
{
    using var _worker = LogContext.PushProperty("WorkerId", workerId);

    Log.Information("Worker {WorkerId} demarre sur le thread {ThreadId}", workerId, Environment.CurrentManagedThreadId);

    for (var step = 1; step <= 5; step++)
    {
        Log.Debug("Worker {WorkerId} traite l'etape {Step}", workerId, step);
        await Task.Delay(Random.Shared.Next(150, 500));
    }

    if (workerId == 3)
    {
        try
        {
            var value = 10 / int.Parse("0");
            Log.Information("Valeur calculee: {Value}", value);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Worker {WorkerId} a rencontre une erreur", workerId);
        }
    }

    Log.Information("Worker {WorkerId} termine", workerId);
}