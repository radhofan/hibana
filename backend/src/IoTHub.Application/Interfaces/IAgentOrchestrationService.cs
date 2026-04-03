using IoTHub.Application.DTOs;

namespace IoTHub.Application.Interfaces;

public interface IAgentOrchestrationService
{
    Task<AgentAnalysisResult> AnalyzeAlertAsync(
        Guid alertId,
        string deviceName,
        string message,
        double triggerValue,
        double threshold,
        CancellationToken ct = default);
}
