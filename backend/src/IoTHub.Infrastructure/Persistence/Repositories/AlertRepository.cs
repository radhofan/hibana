using IoTHub.Application.Interfaces;
using IoTHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTHub.Infrastructure.Persistence.Repositories;

public class AlertRepository(AppDbContext db) : IAlertRepository
{
    public async Task<Alert> AddAsync(Alert alert, CancellationToken ct = default)
    {
        db.Alerts.Add(alert);
        await db.SaveChangesAsync(ct);
        return alert;
    }

    public async Task<(List<Alert> Items, int Total)> GetPagedAsync(int page, int size, CancellationToken ct = default)
    {
        var query = db.Alerts.Include(a => a.Device).OrderByDescending(a => a.TriggeredAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(page * size).Take(size).ToListAsync(ct);
        return (items, total);
    }

    public Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Alerts.Include(a => a.Device).FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task UpdateAsync(Alert alert, CancellationToken ct = default)
    {
        db.Alerts.Update(alert);
        return db.SaveChangesAsync(ct);
    }
}
