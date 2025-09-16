var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

var webpi = builder.AddProject<Projects.AspireAppSvelteSwapOrderItemPOC_WebApi>("webapi")
	.WithReference(postgresdb)
	.WaitFor(postgresdb);

//frontend nodejs from AspireAppSvelteSwapOrderItemPOC.Svelte
builder.AddNpmApp("frontend", "../AspireAppSvelteSwapOrderItemPOC.Svelte", "dev")
	.WithReference(webpi)
	.WaitFor(webpi)
	.WithHttpEndpoint(env: "VITE_PORT")
	.WithExternalHttpEndpoints()
	.PublishAsDockerFile();


builder.Build().Run();
