var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server database with persistent lifetime
// TODO: mettre le nom de la base de données en paramètre pour le voir facilement
var db = builder.AddDatabase("OrderDB");

// Setup backend
var apiService = builder.AddProject<Projects.EFCoreSPMultiResultSet_WebApi>("webapi")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
