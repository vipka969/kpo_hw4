namespace PaymentsService.Infrastructure.Messaging.Kafka;

public interface IKafkaConsumer
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
}