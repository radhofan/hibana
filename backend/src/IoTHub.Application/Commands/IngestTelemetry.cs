using IoTHub.Application.DTOs;
using IoTHub.Application.Interfaces;
using IoTHub.Domain.Entities;
using MediatR;

namespace IoTHub.Application.Commands;

public record IngestTelemetryCommand(
    string HardwareId,
    double Value,
    string Unit,
    DateTime Timestamp
) : IRequest<TelemetryDto>;

public class IngestTelemetryHandler(
    IDeviceRepository deviceRepo,
    ITelemetryRepository telemetryRepo,
    IAlertRepository alertRepo,
    IMessageBus bus,
    ITelemetryHub hub
) : IRequestHandler<IngestTelemetryCommand, TelemetryDto>
{
    public async Task<TelemetryDto> Handle(IngestTelemetryCommand cmd, CancellationToken ct)
    {
        var device = await deviceRepo.GetByHardwareIdAsync(cmd.HardwareId, ct)
            ?? throw new KeyNotFoundException($"Unknown device: {cmd.HardwareId}");

        // Write side – persist raw reading to SQL Server
        var reading = new TelemetryReading
        {
            DeviceId = device.Id,
            Value = cmd.Value,
            Unit = cmd.Unit,
            Timestamp = cmd.Timestamp,
        };
        reading = await telemetryRepo.AddAsync(reading, ct);

        // Update device last-seen + status
        device.LastSeen = cmd.Timestamp;
        device.Status = cmd.Value >= device.AlertThreshold ? DeviceStatus.Warning : DeviceStatus.Online;
        await deviceRepo.UpdateAsync(device, ct);

        var dto = new TelemetryDto(reading.Id, device.Id, device.Name, reading.Value, reading.Unit, reading.Timestamp);

        // Broadcast via SignalR
        await hub.BroadcastTelemetryAsync(dto);
        await hub.BroadcastDeviceStatusAsync(new DeviceStatusDto(device.Id, device.Status.ToString(), device.LastSeen));

        // Threshold breach → queue alert creation via RabbitMQ
        if (cmd.Value >= device.AlertThreshold)
        {
            await bus.PublishAsync("alert-queue", new AlertTriggerMessage(
                device.Id, device.Name, cmd.Value, device.AlertThreshold, device.Unit), ct);
        }

        return dto;
    }
}

public record AlertTriggerMessage(Guid DeviceId, string DeviceName, double Value, double Threshold, string Unit);
