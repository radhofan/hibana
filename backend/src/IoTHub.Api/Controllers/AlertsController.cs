using IoTHub.Application.Interfaces;
using IoTHub.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IoTHub.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController(IMediator mediator, IAlertRepository alertRepo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AlertsPageDto>> GetAlerts(
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetAlertsQuery(page, size), ct));

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct)
    {
        var alert = await alertRepo.GetByIdAsync(id, ct);
        if (alert is null) return NotFound();

        alert.IsAcknowledged = true;
        alert.AcknowledgedAt = DateTime.UtcNow;
        await alertRepo.UpdateAsync(alert, ct);
        return NoContent();
    }
}
