using IoTHub.Application.Commands;
using IoTHub.Application.DTOs;
using IoTHub.Application.Interfaces;
using IoTHub.Application.Queries;
using IoTHub.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace IoTHub.Infrastructure.Messaging;

/// <summary>
/// Background worker that consumes alert-queue messages from RabbitMQ,
/// persists alerts to SQL Server, runs AI agent analysis, and broadcasts via SignalR.
/// </summary>
public class AlertProcessorWorker(
    IConnection rabbitConnection,
    IServiceScopeFactory scopeFactory,
    ILogger<AlertProcessorWorker> logger
) : BackgroundService
{
    private const string Queue = "alert-queue";
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg = JsonSerializer.Deserialize<AlertTriggerMessage>(body);
                if (msg is null) return;

                await ProcessAlertAsync(msg, stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process alert message");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        // Keep alive
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessAlertAsync(AlertTriggerMessage msg, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var alertRepo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
        var hub = scope.ServiceProvider.GetRequiredService<ITelemetryHub>();
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentOrchestrationService>();

        var severity = msg.Value >= msg.Threshold * 1.5 ? AlertSeverity.Critical : AlertSeverity.Warning;

        var alert = new Alert
        {
            DeviceId = msg.DeviceId,
            Message = $"Value {msg.Value:F1} {msg.Unit} exceeded threshold {msg.Threshold:F1} {msg.Unit}",
            Severity = severity,
            TriggerValue = msg.Value,
            Threshold = msg.Threshold,
        };

        alert = await alertRepo.AddAsync(alert, ct);
        logger.LogWarning("[ALERT] Device {DeviceId}: {Message}", msg.DeviceId, alert.Message);

        await hub.BroadcastAlertAsync(new AlertDto(
            alert.Id, alert.DeviceId, msg.DeviceName,
            alert.Message, alert.Severity.ToString(),
            alert.TriggerValue, alert.Threshold,
            false, alert.TriggeredAt));

        // ── AI agent analysis ─────────────────────────────────────────────────────
        try
        {
            var analysis = await agentService.AnalyzeAlertAsync(
                alert.Id,
                msg.DeviceName,
                alert.Message,
                msg.Value,
                msg.Threshold,
                ct);

            var analysisDto = new AgentAnalysisDto(
                alert.Id,
                analysis.PlannerAssessment,
                analysis.ReviewerCritique,
                analysis.RecommendedAction,
                DateTime.UtcNow);

            alert.AgentAnalysis = JsonSerializer.Serialize(analysisDto);
            await alertRepo.UpdateAsync(alert, ct);

            await hub.BroadcastAgentAnalysisAsync(analysisDto, ct);

            logger.LogInformation("[AI] Agent analysis saved for alert {AlertId}", alert.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent analysis step failed for alert {AlertId}; alert already persisted", alert.Id);
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        await base.StopAsync(ct);
    }
}
