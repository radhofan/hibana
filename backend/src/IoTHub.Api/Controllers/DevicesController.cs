using IoTHub.Application.Commands;
using IoTHub.Application.DTOs;
using IoTHub.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IoTHub.Api.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<DeviceDto>>> GetAll(CancellationToken ct) =>
        Ok(await mediator.Send(new GetDevicesQuery(), ct));

    [HttpPost]
    public async Task<ActionResult<DeviceDto>> Register(
        [FromBody] RegisterDeviceRequest req, CancellationToken ct)
    {
        var dto = await mediator.Send(new RegisterDeviceCommand(
            req.HardwareId, req.Name, req.Latitude, req.Longitude,
            req.AlertThreshold, req.Unit), ct);
        return CreatedAtAction(nameof(GetAll), dto);
    }

    [HttpPut("{id:guid}/threshold")]
    public async Task<IActionResult> UpdateThreshold(
        Guid id, [FromBody] UpdateThresholdRequest req, CancellationToken ct)
    {
        await mediator.Send(new UpdateThresholdCommand(id, req.Threshold), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/telemetry")]
    public async Task<ActionResult<List<TelemetryDto>>> GetTelemetry(
        Guid id, [FromQuery] int hours = 24, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetTelemetryQuery(id, hours), ct));
}

public record RegisterDeviceRequest(
    string HardwareId,
    string Name,
    double Latitude,
    double Longitude,
    double AlertThreshold,
    string Unit = "°C");

public record UpdateThresholdRequest(double Threshold);
