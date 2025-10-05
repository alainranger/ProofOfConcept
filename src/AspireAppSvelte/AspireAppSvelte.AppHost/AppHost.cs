var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireAppSvelte_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

// Service Svelte Frontend
var svelteApp = builder.AddExecutable("svelte-frontend", "npm", "../AspireAppSvelte.Svelte", "run", "dev")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
