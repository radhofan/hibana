namespace IoTHub.Domain.Entities;

public class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string HardwareId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Offline;
    public double AlertThreshold { get; set; } = 100.0;
    public string Unit { get; set; } = "°C";
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TelemetryReading> Readings { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];
}

public enum DeviceStatus { Offline, Online, Warning, Error }
