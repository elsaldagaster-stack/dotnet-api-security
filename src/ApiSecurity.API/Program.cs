using ApiSecurity.API.Endpoints;
using ApiSecurity.API.Extensions;
using ApiSecurity.API.Middleware;
using ApiSecurity.Application.Common;
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Application.Webhooks;
using ApiSecurity.Infrastructure.Persistence;
using ApiSecurity.Infrastructure.Repositories;
using ApiSecurity.Infrastructure.Security;
using ApiSecurity.Infrastructure.Webhooks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console()
       .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddSingleton<IApiKeyHasher, ApiKeyHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services.AddScoped<IWebhookRepository, WebhookRepository>();
builder.Services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();
builder.Services.AddScoped<IWebhookSignatureService, WebhookSignatureService>();
builder.Services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();
builder.Services.AddHostedService<WebhookDeliveryWorker>();
builder.Services.AddHttpClient("webhook").ConfigureHttpClient(c =>
    c.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ApiSecurity.Application.ApiKeys.Commands.CreateApiKeyCommand).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(ApiSecurity.Application.ApiKeys.Commands.CreateApiKeyCommand).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiRateLimiting();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!);

builder.Services.AddOpenApi();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ApiSecurity"))
        .AddAspNetCoreInstrumentation()
        .AddJaegerExporter());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<IpFilterMiddleware>();
app.UseMiddleware<AuditLogMiddleware>();

app.UseCors(CorsExtensions.PolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapAuthEndpoints();
app.MapApiKeyEndpoints();
app.MapProductEndpoints();
app.MapWebhookEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
