using Confluent.Kafka;

namespace cinema.events.Kafka.Producer
{
    /// <summary>
    ///     Wraps a Confluent.Kafka.IProducer instance, and allows for basic
    ///     configuration of this via IConfiguration.
    ///    
    ///     KafkaClientHandle does not provide any way for messages to be produced
    ///     directly. Instead, it is a dependency of KafkaDependentProducer. You
    ///     can create more than one instance of KafkaDependentProducer (with
    ///     possibly differing K and V generic types) that leverages the same
    ///     underlying producer instance exposed by the Handle property of this
    ///     class. This is more efficient than creating separate
    ///     Confluent.Kafka.IProducer instances for each Message type you wish to
    ///     produce.
    /// </summary>
    public class KafkaClientHandle : IDisposable
    {
        IProducer<byte[], byte[]> kafkaProducer;

        public KafkaClientHandle(IConfiguration config)
        {
            var kafkaBrokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(kafkaBrokers);

            var conf = new ProducerConfig()
            {
                BootstrapServers = kafkaBrokers
            };

            kafkaProducer = new ProducerBuilder<byte[], byte[]>(conf).Build();
        }

        public Handle Handle { get => kafkaProducer.Handle; }

        public void Dispose()
        {
            // Block until all outstanding produce requests have completed (with or
            // without error).
            kafkaProducer.Flush();
            kafkaProducer.Dispose();
        }
    }
}
