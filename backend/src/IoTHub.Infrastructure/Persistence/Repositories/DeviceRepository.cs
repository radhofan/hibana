using IoTHub.Application.Interfaces;
using IoTHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTHub.Infrastructure.Persistence.Repositories;

public class DeviceRepository(AppDbContext db) : IDeviceRepository
{
    public Task<Device?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Devices.FindAsync([id], ct).AsTask();

    public Task<Device?> GetByHardwareIdAsync(string hardwareId, CancellationToken ct = default) =>
        db.Devices.FirstOrDefaultAsync(d => d.HardwareId == hardwareId, ct);

    public Task<List<Device>> GetAllAsync(CancellationToken ct = default) =>
        db.Devices.OrderBy(d => d.Name).ToListAsync(ct);

    public async Task<Device> AddAsync(Device device, CancellationToken ct = default)
    {
        db.Devices.Add(device);
        await db.SaveChangesAsync(ct);
        return device;
    }

    public Task UpdateAsync(Device device, CancellationToken ct = default)
    {
        db.Devices.Update(device);
        return db.SaveChangesAsync(ct);
    }
}
