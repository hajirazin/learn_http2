using System.Text.Json;
using Http2Streaming.Api.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

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

// Enable CORS
app.UseCors();

app.MapGet("/", () => Results.Text("HTTP/2 streaming demo backend is running."));

// NDJSON streaming endpoint: one JSON object per line.
app.MapGet("/api/records/stream", async context =>
{
    context.Response.StatusCode = StatusCodes.Status200OK;
    context.Response.ContentType = "application/x-ndjson";

    var cancellationToken = context.RequestAborted;

    await foreach (var record in RecordGenerator.GenerateRecords(cancellationToken: cancellationToken))
    {
        var json = JsonSerializer.Serialize(record);
        await context.Response.WriteAsync(json + "\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
});

app.Run();


