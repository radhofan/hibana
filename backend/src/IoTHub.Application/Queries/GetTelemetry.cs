using IoTHub.Application.DTOs;
using IoTHub.Application.Interfaces;
using IoTHub.Domain.Entities;
using MediatR;

namespace IoTHub.Application.Queries;

public record GetTelemetryQuery(Guid DeviceId, int Hours = 24) : IRequest<List<TelemetryDto>>;

public class GetTelemetryHandler(ITelemetryRepository repo, IDeviceRepository deviceRepo)
    : IRequestHandler<GetTelemetryQuery, List<TelemetryDto>>
{
    public async Task<List<TelemetryDto>> Handle(GetTelemetryQuery q, CancellationToken ct)
    {
        var device = await deviceRepo.GetByIdAsync(q.DeviceId, ct)
            ?? throw new KeyNotFoundException($"Device {q.DeviceId} not found.");

        var readings = await repo.GetByDeviceAsync(q.DeviceId, q.Hours, ct);
        return readings.Select(r => MapToDto(r, device)).ToList();
    }

    private static TelemetryDto MapToDto(TelemetryReading r, Device d) =>
        new(r.Id, d.Id, d.Name, r.Value, r.Unit, r.Timestamp);
}
