using IoTHub.Application.DTOs;
using IoTHub.Application.Interfaces;
using IoTHub.Domain.Entities;
using MediatR;

namespace IoTHub.Application.Queries;

public record GetAlertsQuery(int Page = 0, int Size = 20) : IRequest<AlertsPageDto>;
public record AlertsPageDto(List<AlertDto> Items, int Total);

public class GetAlertsHandler(IAlertRepository repo) : IRequestHandler<GetAlertsQuery, AlertsPageDto>
{
    public async Task<AlertsPageDto> Handle(GetAlertsQuery q, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(q.Page, q.Size, ct);
        return new AlertsPageDto(items.Select(MapToDto).ToList(), total);
    }

    internal static AlertDto MapToDto(Alert a) => new(
        a.Id, a.DeviceId, a.Device?.Name ?? "Unknown",
        a.Message, a.Severity.ToString(),
        a.TriggerValue, a.Threshold,
        a.IsAcknowledged, a.TriggeredAt);
}
