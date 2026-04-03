using IoTHub.Application.Commands;
using IoTHub.Application.DTOs;
using IoTHub.Application.Interfaces;
using MediatR;

namespace IoTHub.Application.Queries;

public record GetDevicesQuery : IRequest<List<DeviceDto>>;

public class GetDevicesHandler(IDeviceRepository repo, ICacheService cache)
    : IRequestHandler<GetDevicesQuery, List<DeviceDto>>
{
    public Task<List<DeviceDto>> Handle(GetDevicesQuery _, CancellationToken ct) =>
        cache.GetOrSetAsync(
            "devices:all",
            async () => (await repo.GetAllAsync(ct))
                .Select(RegisterDeviceHandler.MapToDto)
                .ToList(),
            TimeSpan.FromSeconds(30),
            ct);
}
