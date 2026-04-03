using IoTHub.Domain.Entities;

namespace IoTHub.Application.Interfaces;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Device?> GetByHardwareIdAsync(string hardwareId, CancellationToken ct = default);
    Task<List<Device>> GetAllAsync(CancellationToken ct = default);
    Task<Device> AddAsync(Device device, CancellationToken ct = default);
    Task UpdateAsync(Device device, CancellationToken ct = default);
}
