using Confluent.Kafka;

namespace cinema.events.Backgroung
{
    public class KafkaConsumerBackgroundService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerBackgroundService> _logger;
        private ConsumerConfig _consumerConfig;
        private string[] _topics;

        public KafkaConsumerBackgroundService(ILogger<KafkaConsumerBackgroundService> logger)
        {
            _logger = logger;

            var kafkaBrokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(kafkaBrokers);
            _logger.LogDebug(kafkaBrokers);

            _consumerConfig = new ConsumerConfig
            {
                // User-specific properties that you must set
                BootstrapServers = kafkaBrokers,
                GroupId = "kafka.cinema.events.created",
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            var kafkaTopics = Environment.GetEnvironmentVariable("KAFKA_TOPICS");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(kafkaTopics);
            _logger.LogDebug(kafkaTopics);

            _topics = kafkaTopics.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var task = Task.Run(() =>
            {

                try
                {
                    using (var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build())
                    {
                        consumer.Subscribe(_topics);
                        try
                        {
                            while (!stoppingToken.IsCancellationRequested)
                            {
                                var consumResult = consumer.Consume(stoppingToken);

                                _logger.LogDebug(
                                    $"Consumed event from topic {consumResult.Topic}: " +
                                    $"key = {consumResult.Message.Key,-10} " +
                                    $"value = {consumResult.Message.Value}");
                            }
                        }
                        finally
                        {
                            consumer.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "");
                }
            });

            return task;
        }
    }
}
