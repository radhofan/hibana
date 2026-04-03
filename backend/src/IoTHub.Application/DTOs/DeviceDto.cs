using IoTHub.Domain.Entities;

namespace IoTHub.Application.DTOs;

public record DeviceDto(
    Guid Id,
    string HardwareId,
    string Name,
    double Latitude,
    double Longitude,
    string Status,
    double AlertThreshold,
    string Unit,
    DateTime LastSeen
);

public record TelemetryDto(
    long Id,
    Guid DeviceId,
    string DeviceName,
    double Value,
    string Unit,
    DateTime Timestamp
);

public record AlertDto(
    Guid Id,
    Guid DeviceId,
    string DeviceName,
    string Message,
    string Severity,
    double TriggerValue,
    double Threshold,
    bool IsAcknowledged,
    DateTime TriggeredAt
);

public record DeviceStatusDto(Guid DeviceId, string Status, DateTime LastSeen);
