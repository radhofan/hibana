using IoTHub.Application.DTOs;

namespace IoTHub.Application.Interfaces;

public interface ITelemetryHub
{
    Task BroadcastTelemetryAsync(TelemetryDto reading);
    Task BroadcastAlertAsync(AlertDto alert);
    Task BroadcastDeviceStatusAsync(DeviceStatusDto status);
    Task BroadcastAgentAnalysisAsync(AgentAnalysisDto analysis, CancellationToken ct = default);
}
