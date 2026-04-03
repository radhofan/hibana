using IoTHub.Application.Interfaces;
using IoTHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTHub.Infrastructure.Persistence.Repositories;

public class TelemetryRepository(AppDbContext db) : ITelemetryRepository
{
    public async Task<TelemetryReading> AddAsync(TelemetryReading reading, CancellationToken ct = default)
    {
        db.TelemetryReadings.Add(reading);
        await db.SaveChangesAsync(ct);
        return reading;
    }

    public Task<List<TelemetryReading>> GetByDeviceAsync(Guid deviceId, int hours = 24, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddHours(-hours);
        return db.TelemetryReadings
            .Where(r => r.DeviceId == deviceId && r.Timestamp >= since)
            .OrderBy(r => r.Timestamp)
            .ToListAsync(ct);
    }

    public Task<List<TelemetryReading>> GetRecentAsync(int count = 100, CancellationToken ct = default) =>
        db.TelemetryReadings
            .Include(r => r.Device)
            .OrderByDescending(r => r.Timestamp)
            .Take(count)
            .ToListAsync(ct);
}
