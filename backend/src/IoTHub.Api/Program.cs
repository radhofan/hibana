using IoTHub.AI;
using IoTHub.Application.Interfaces;
using IoTHub.Api.Grpc;
using IoTHub.Api.Hubs;
using IoTHub.Infrastructure;
using IoTHub.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ── Sentry ────────────────────────────────────────────────
builder.WebHost.UseSentry(o =>
{
    o.Dsn = builder.Configuration["Sentry:Dsn"];
    o.TracesSampleRate = 1.0;
});

// ── Services ──────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Clean Architecture layers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IoTHub.Application.Commands.RegisterDeviceCommand).Assembly));

builder.Services.AddInfrastructure(builder.Configuration);

// AI agent orchestration (singleton – Kernel is thread-safe)
builder.Services.AddSingleton<IAgentOrchestrationService, AgentOrchestrationService>();

// SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<ITelemetryHub, SignalRTelemetryHub>();

// gRPC
builder.Services.AddGrpc();

// CORS – allow React dev server
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// ── App ───────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRouting();

// Sentry request tracing
app.UseSentryTracing();

// Prometheus metrics endpoint
app.UseHttpMetrics();
app.MapMetrics("/metrics");

app.MapControllers();
app.MapHub<TelemetrySignalRHub>("/hubs/telemetry");
app.MapGrpcService<TelemetryGrpcService>();

app.Run();
