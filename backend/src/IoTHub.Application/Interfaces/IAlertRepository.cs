using IoTHub.Domain.Entities;

namespace IoTHub.Application.Interfaces;

public interface IAlertRepository
{
    Task<Alert> AddAsync(Alert alert, CancellationToken ct = default);
    Task<(List<Alert> Items, int Total)> GetPagedAsync(int page, int size, CancellationToken ct = default);
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Alert alert, CancellationToken ct = default);
}
