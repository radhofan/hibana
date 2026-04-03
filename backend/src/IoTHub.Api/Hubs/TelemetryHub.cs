using IoTHub.Application.DTOs;
using IoTHub.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace IoTHub.Api.Hubs;

/// <summary>
/// SignalR hub – frontend connects here for real-time telemetry, alerts, and device status.
/// Events pushed: TelemetryReceived, AlertTriggered, DeviceStatusChanged
/// </summary>
public class TelemetrySignalRHub : Hub
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }
}

/// <summary>
/// Service that wraps IHubContext to broadcast from application layer via ITelemetryHub.
/// </summary>
public class SignalRTelemetryHub(IHubContext<TelemetrySignalRHub> context) : ITelemetryHub
{
    public Task BroadcastTelemetryAsync(TelemetryDto reading) =>
        context.Clients.All.SendAsync("TelemetryReceived", reading);

    public Task BroadcastAlertAsync(AlertDto alert) =>
        context.Clients.All.SendAsync("AlertTriggered", alert);

    public Task BroadcastDeviceStatusAsync(DeviceStatusDto status) =>
        context.Clients.All.SendAsync("DeviceStatusChanged", status);

    public Task BroadcastAgentAnalysisAsync(AgentAnalysisDto analysis, CancellationToken ct = default) =>
        context.Clients.All.SendAsync(
            "AgentAnalysisReady",
            new
            {
                alertId = analysis.AlertId,
                plannerAssessment = analysis.PlannerAssessment,
                reviewerCritique = analysis.ReviewerCritique,
                recommendedAction = analysis.RecommendedAction
            },
            ct);
}
