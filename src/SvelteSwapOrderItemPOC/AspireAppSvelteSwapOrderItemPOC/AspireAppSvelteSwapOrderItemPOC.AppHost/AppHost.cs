var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

builder.AddProject<Projects.AspireAppSvelteSwapOrderItemPOC_WebApi>("aspireappsvelteswaporderitempoc-webapi")
	.WithReference(postgresdb)
	.WaitFor(postgresdb);



builder.Build().Run();
