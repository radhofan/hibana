using IoTHub.Application.Interfaces;
using MediatR;

namespace IoTHub.Application.Commands;

public record UpdateThresholdCommand(Guid DeviceId, double Threshold) : IRequest;

public class UpdateThresholdHandler(IDeviceRepository repo, ICacheService cache)
    : IRequestHandler<UpdateThresholdCommand>
{
    public async Task Handle(UpdateThresholdCommand cmd, CancellationToken ct)
    {
        var device = await repo.GetByIdAsync(cmd.DeviceId, ct)
            ?? throw new KeyNotFoundException($"Device {cmd.DeviceId} not found.");

        device.AlertThreshold = cmd.Threshold;
        await repo.UpdateAsync(device, ct);
        await cache.RemoveAsync("devices:all", ct);
    }
}
