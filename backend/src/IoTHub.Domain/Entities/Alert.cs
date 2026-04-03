namespace IoTHub.Domain.Entities;

public class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public double TriggerValue { get; set; }
    public double Threshold { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public string? AgentAnalysis { get; set; }  // JSON of AgentAnalysisDto
}

public enum AlertSeverity { Info, Warning, Critical }
