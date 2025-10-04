var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireAppSvelte_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
