namespace IoTHub.Domain.Entities;

public class TelemetryReading
{
    public long Id { get; set; }
    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
