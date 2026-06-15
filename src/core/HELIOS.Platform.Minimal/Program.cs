using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapGet("/api/status", () => Results.Ok(new
{
    service = "HELIOS Platform",
    status = "READY",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/", () => Results.Ok(new
{
    service = "HELIOS Platform",
    status = "READY"
}));

app.Run();
