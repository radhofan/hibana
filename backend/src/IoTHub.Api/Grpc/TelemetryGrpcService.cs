using Grpc.Core;
using IoTHub.Application.Commands;
using MediatR;

namespace IoTHub.Api.Grpc;

public class TelemetryGrpcService(IMediator mediator, ILogger<TelemetryGrpcService> logger)
    : TelemetryService.TelemetryServiceBase
{
    public override async Task<TelemetryResponse> IngestReading(
        TelemetryRequest request, ServerCallContext context)
    {
        try
        {
            var cmd = Map(request);
            var dto = await mediator.Send(cmd, context.CancellationToken);
            return new TelemetryResponse { ReadingId = dto.Id.ToString(), Status = "OK" };
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning("gRPC IngestReading: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "gRPC IngestReading failed");
            throw new RpcException(new Status(StatusCode.Internal, "Internal error"));
        }
    }

    public override async Task<IngestStreamResponse> IngestStream(
        IAsyncStreamReader<TelemetryRequest> requestStream, ServerCallContext context)
    {
        int accepted = 0, rejected = 0;

        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            try
            {
                await mediator.Send(Map(request), context.CancellationToken);
                accepted++;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Stream ingest rejected: {Msg}", ex.Message);
                rejected++;
            }
        }

        return new IngestStreamResponse { Accepted = accepted, Rejected = rejected };
    }

    private static IngestTelemetryCommand Map(TelemetryRequest r) => new(
        r.HardwareId,
        r.Value,
        r.Unit,
        DateTimeOffset.FromUnixTimeMilliseconds(r.TimestampUnixMs).UtcDateTime);
}
