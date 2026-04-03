using IoTHub.Domain.Entities;

namespace IoTHub.Application.Interfaces;

public interface ITelemetryRepository
{
    Task<TelemetryReading> AddAsync(TelemetryReading reading, CancellationToken ct = default);
    Task<List<TelemetryReading>> GetByDeviceAsync(Guid deviceId, int hours = 24, CancellationToken ct = default);
    Task<List<TelemetryReading>> GetRecentAsync(int count = 100, CancellationToken ct = default);
}
