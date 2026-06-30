using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "HELIOS Platform",
    status = "running",
    version = "1.0.0"
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/status", () => Results.Ok(new
{
    platform = "HELIOS",
    status = "ready",
    runtime = ".NET 8.0",
    processId = Environment.ProcessId
}));

app.Run();
