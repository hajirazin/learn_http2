using System.Text.Json;
using Http2Streaming.Api.Data;
using Http2Streaming.Api.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy for local development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add DbContext with Npgsql
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

// Configure Kestrel explicitly for HTTP/2 over HTTPS on localhost:5001
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001, listenOptions =>
    {
        listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

// Apply migrations on startup with retry logic
await ApplyMigrationsWithRetry(app.Services);

async Task ApplyMigrationsWithRetry(IServiceProvider services, int maxRetries = 5)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Database migrations applied successfully.");
            return;
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to apply migrations (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}s...", 
                i + 1, maxRetries, Math.Pow(2, i));
            
            if (i == maxRetries - 1)
            {
                app.Logger.LogError(ex, "Failed to apply migrations after {MaxRetries} attempts.", maxRetries);
                throw;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
        }
    }
}

// Enable CORS
app.UseCors();

app.MapGet("/", () => Results.Text("HTTP/2 streaming demo backend is running."));

// Health check endpoint with database connectivity check
app.MapGet("/health", async (AppDbContext dbContext) =>
{
    try
    {
        // Check if database is accessible
        var canConnect = await dbContext.Database.CanConnectAsync();
        
        if (!canConnect)
        {
            return Results.Json(new
            {
                status = "Unhealthy",
                database = "Cannot connect",
                timestamp = DateTime.UtcNow
            }, statusCode: 503);
        }

        // Check if we can query the database
        var recordCount = await dbContext.Records.CountAsync();

        return Results.Json(new
        {
            status = "Healthy",
            database = "Connected",
            recordCount = recordCount,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "Unhealthy",
            database = "Error",
            error = ex.Message,
            timestamp = DateTime.UtcNow
        }, statusCode: 503);
    }
});

// NDJSON streaming endpoint: stream records from database
app.MapGet("/api/records/stream", async (AppDbContext dbContext, HttpContext context) =>
{
    context.Response.StatusCode = StatusCodes.Status200OK;
    context.Response.ContentType = "application/x-ndjson";

    var cancellationToken = context.RequestAborted;

    await foreach (var recordEntity in dbContext.Records
        .AsNoTracking()
        .OrderBy(r => r.Id)
        .AsAsyncEnumerable()
        .WithCancellation(cancellationToken))
    {
        // Map entity to DTO
        var record = new RecordDto(
            Id: recordEntity.Id,
            Name: recordEntity.Name,
            Value: recordEntity.Value,
            CreatedAt: recordEntity.CreatedAt);

        var json = JsonSerializer.Serialize(record);
        await context.Response.WriteAsync(json + "\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
});

app.Run();


