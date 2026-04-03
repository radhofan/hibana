using IoTHub.Application.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace IoTHub.Infrastructure.Messaging;

public class RabbitMqPublisher(IConnection connection) : IMessageBus, IAsyncDisposable
{
    private readonly IChannel _channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

    public async Task PublishAsync<T>(string queue, T message, CancellationToken ct = default)
    {
        await _channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = new BasicProperties { Persistent = true };

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queue,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
    }
}
