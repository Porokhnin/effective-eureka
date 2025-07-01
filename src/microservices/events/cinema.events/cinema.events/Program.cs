using cinema.events.Backgroung;
using cinema.events.Kafka.Producer;
using cinema.events.Models.Requests;
using Microsoft.AspNetCore.Http.Json;
using System.Net;
using System.Text.Json;

namespace cinema.events
{
    public class Program
    {
        internal static int DEFAULT_PORT = 8082;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            var hostPort = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out int intParsedResult) ? intParsedResult : DEFAULT_PORT;
            builder.WebHost.ConfigureKestrel((context, serverOptions) =>
            {
                serverOptions.Listen(System.Net.IPAddress.Any, hostPort);
            });

            builder.Services.AddSingleton<KafkaClientHandle>();
            builder.Services.AddSingleton<KafkaDependentProducer<string, string>>();
            builder.Services.AddHostedService<KafkaConsumerBackgroundService>();

            //snake - camel
            builder.Services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                options.SerializerOptions.WriteIndented = true;
            });


            var app = builder.Build();

            //---------------------------------------------------------------------------------


            app.MapGet("/api/events/health", () =>
            {
                return Results.Ok(new { status = true });
            });

            app.MapPost("/api/events/user", async (CreateUserEventRequest request, KafkaDependentProducer<string, string> producer, ILogger<Program> logger) =>
            {
                logger.LogDebug($"{request}");

                var userCreated = new UserCreated()
                {
                    UserId = request.UserId,
                    Username = request.Username,
                    Timestamp = request.Timestamp,
                    Action = request.Action
                };

                await producer.ProduceAsync("user-events", new Confluent.Kafka.Message<string, string>() { Key = "user", Value = JsonSerializer.Serialize(userCreated) });

                return Results.Json(data: new { status = "success" }, statusCode: StatusCodes.Status201Created);
            });

            app.MapPost("/api/events/payment", async (CreatePaymentEventRequest request, KafkaDependentProducer<string, string> producer, ILogger<Program> logger) =>
            {
                logger.LogDebug($"{request}");

                var paymentCreated = new PaymentCreated()
                {
                    PaymentId = request.UserId,
                    UserId = request.UserId,
                    MethodType = request.MethodType,
                    Status = request.Status,
                    Amount = request.Amount,
                    Timestamp = request.Timestamp
                };

                await producer.ProduceAsync("payment-events", new Confluent.Kafka.Message<string, string>() { Key = "payment", Value = JsonSerializer.Serialize(paymentCreated) });

                return Results.Json(data: new { status = "success" }, statusCode: StatusCodes.Status201Created);
            });

            app.MapPost("/api/events/movie", async (CreateMovieEventRequest request, KafkaDependentProducer<string, string> producer, ILogger<Program> logger) =>
            {
                logger.LogDebug($"{request}");

                var movieCreated = new MovieCreated()
                {
                    MovieId = request.MovieId,
                    UserId = request.UserId,
                    Title = request.Title,
                    Action = request.Action
                };

                await producer.ProduceAsync("movie-events", new Confluent.Kafka.Message<string, string>() { Key = "movie", Value = JsonSerializer.Serialize(movieCreated) });

                return Results.Json(data: new { status = "success" }, statusCode: StatusCodes.Status201Created);
            });
            //---------------------------------------------------------------------------------

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.Run();
        }
    }
}
