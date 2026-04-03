namespace IoTHub.Application.Interfaces;

public interface IMessageBus
{
    Task PublishAsync<T>(string queue, T message, CancellationToken ct = default);
}
