using IoTHub.Application.Commands;
using IoTHub.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IoTHub.Api.Controllers;

/// <summary>
/// REST fallback for telemetry ingestion (gRPC is the primary high-throughput path).
/// </summary>
[ApiController]
[Route("api/telemetry")]
public class TelemetryController(IMediator mediator) : ControllerBase
{
    [HttpPost("ingest")]
    public async Task<ActionResult<TelemetryDto>> Ingest(
        [FromBody] IngestRequest req, CancellationToken ct)
    {
        var dto = await mediator.Send(new IngestTelemetryCommand(
            req.HardwareId, req.Value, req.Unit,
            req.Timestamp ?? DateTime.UtcNow), ct);
        return Ok(dto);
    }
}

public record IngestRequest(
    string HardwareId,
    double Value,
    string Unit = "°C",
    DateTime? Timestamp = null);
