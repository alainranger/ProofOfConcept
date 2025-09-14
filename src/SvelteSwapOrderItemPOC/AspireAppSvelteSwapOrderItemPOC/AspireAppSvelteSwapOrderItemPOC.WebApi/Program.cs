using AspireAppSvelteSwapOrderItemPOC.WebApi.Database;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<OrderbBContext>(connectionName: "postgresdb");

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<OrderbBContext>();
	await context.Database.EnsureCreatedAsync().ConfigureAwait(true);

	var services = scope.ServiceProvider;
	SeedData.Initialize(services);
}

app.UseHttpsRedirection();

app.MapGet("/items", async (OrderbBContext db) =>
	await db.Items
		.OrderBy(i => i.Position)
		.ToListAsync()
		.ConfigureAwait(true));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
