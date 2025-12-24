namespace PaymentsService.Infrastructure.Messaging.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync(string topic, string key, object message);
}