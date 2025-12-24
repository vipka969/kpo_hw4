using Confluent.Kafka;

namespace OrderService.Infrastructure.Messaging.Kafka;

public sealed class KafkaProducer
{
    private readonly IProducer<string, string> producer;

    public KafkaProducer(string bootstrapServers)
    {
        ProducerConfig config = new ProducerConfig();
        config.BootstrapServers = bootstrapServers;

        producer = new ProducerBuilder<string, string>(config).Build();
    }

    public Task ProduceAsync(string topic, string key, string payload)
    {
        Message<string, string> message = new Message<string, string>();
        message.Key = key;
        message.Value = payload;

        return producer.ProduceAsync(topic, message);
    }
}