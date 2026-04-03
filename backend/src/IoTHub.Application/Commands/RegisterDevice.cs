using IoTHub.Application.DTOs;
using IoTHub.Application.Interfaces;
using IoTHub.Domain.Entities;
using MediatR;

namespace IoTHub.Application.Commands;

public record RegisterDeviceCommand(
    string HardwareId,
    string Name,
    double Latitude,
    double Longitude,
    double AlertThreshold,
    string Unit
) : IRequest<DeviceDto>;

public class RegisterDeviceHandler(IDeviceRepository repo, ICacheService cache)
    : IRequestHandler<RegisterDeviceCommand, DeviceDto>
{
    public async Task<DeviceDto> Handle(RegisterDeviceCommand cmd, CancellationToken ct)
    {
        var existing = await repo.GetByHardwareIdAsync(cmd.HardwareId, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Device with HardwareId '{cmd.HardwareId}' already exists.");

        var device = new Device
        {
            HardwareId = cmd.HardwareId,
            Name = cmd.Name,
            Latitude = cmd.Latitude,
            Longitude = cmd.Longitude,
            AlertThreshold = cmd.AlertThreshold,
            Unit = cmd.Unit,
        };

        device = await repo.AddAsync(device, ct);
        await cache.RemoveAsync("devices:all", ct);

        return MapToDto(device);
    }

    internal static DeviceDto MapToDto(Device d) => new(
        d.Id, d.HardwareId, d.Name, d.Latitude, d.Longitude,
        d.Status.ToString(), d.AlertThreshold, d.Unit, d.LastSeen);
}
