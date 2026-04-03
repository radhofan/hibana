using IoTHub.Application.Interfaces;
using IoTHub.Infrastructure.Caching;
using IoTHub.Infrastructure.Messaging;
using IoTHub.Infrastructure.Persistence;
using IoTHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace IoTHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // SQL Server (Write side)
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("SqlServer")));

        // Redis (Read side / Cache)
        var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConn));
        services.AddScoped<ICacheService, RedisCacheService>();

        // RabbitMQ
        services.AddSingleton<IConnection>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
                UserName = config["RabbitMQ:Username"] ?? "guest",
                Password = config["RabbitMQ:Password"] ?? "guest",
            };
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });
        services.AddScoped<IMessageBus, RabbitMqPublisher>();
        services.AddHostedService<AlertProcessorWorker>();

        // Repositories
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<ITelemetryRepository, TelemetryRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        return services;
    }
}
