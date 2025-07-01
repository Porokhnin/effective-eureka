using Microsoft.AspNetCore.Http.Json;
using System.Security.Authentication;
using System.Text.Json;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;

namespace cinema.proxy
{
    public class Program
    {
        internal static int DEFAULT_PORT = 8000;

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

            var proxyConfig = CinfigureProxy();
            builder.Services.AddReverseProxy().LoadFromMemory(proxyConfig.routes, proxyConfig.clusters);
            builder.Services.AddSingleton<ILoadBalancingPolicy, ProcentLoadBalancingPolicy>();

            //snake - camel
            builder.Services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                options.SerializerOptions.WriteIndented = true;
            });

            var app = builder.Build();

            //---------------------------------------------------------------------------------

            app.MapGet("/health", () =>
            {
                return Results.Ok(new { status = true });
            });

            //---------------------------------------------------------------------------------

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapReverseProxy(proxyPipeline =>
            {
                //proxyPipeline.UseSessionAffinity();
                proxyPipeline.UseLoadBalancing();
            });

            /*
            app.MapReverseProxy(proxyPipeline =>
            {
                // Custom cluster selection
                proxyPipeline.Use((context, next) =>
                {
                    var lookup = context.RequestServices.GetRequiredService<IProxyStateLookup>();
                    if (lookup.TryGetCluster(ChooseCluster(context), out var cluster))
                    {
                        context.ReassignProxyRequest(cluster);
                    }

                    return next();
                });
                proxyPipeline.UseSessionAffinity();
                proxyPipeline.UseLoadBalancing();
            });

            string ChooseCluster(HttpContext context)
            {
                // Decide which cluster to use. This could be random, weighted, based on headers, etc.
                return Random.Shared.Next(2) == 1 ? "cluster1" : "cluster2";
            }
            */

            app.Run();
        }

        private static (RouteConfig[] routes, ClusterConfig[] clusters) CinfigureProxy()
        {
            var gradualMigration = bool.TryParse(Environment.GetEnvironmentVariable("GRADUAL_MIGRATION"), out bool boolParsedResult)
                ? boolParsedResult
                : false;

            var moviesMigrationPercent = int.TryParse(Environment.GetEnvironmentVariable("MOVIES_MIGRATION_PERCENT"), out int intParsedResult)
                ? intParsedResult
                : 50;

            var monolithUrl = Environment.GetEnvironmentVariable("MONOLITH_URL");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(monolithUrl);
            var movieshUrl = Environment.GetEnvironmentVariable("MOVIES_SERVICE_URL");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(movieshUrl);
            var eventsUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(eventsUrl);


            var routes = new[]
            {
                CreatedRoute("movies", "api/movies/{**catch-all}"),
                CreatedRoute("events", "api/events/{**catch-all}"),
                CreatedRoute("others", "{**catch-all}"),
            };

            var moivesCluster = gradualMigration
                ?
                CreateCluster("movies", "percent", new Dictionary<string, DestinationConfig>()
                {
                    {
                        "destination1",
                        new DestinationConfig()
                        {
                            Address = monolithUrl,
                            Metadata = new Dictionary<string, string>() { { "percent", $"{100 - moviesMigrationPercent}" } }
                        }
                    },
                    {
                        "destination2",
                        new DestinationConfig()
                        {
                            Address = movieshUrl,
                            Metadata = new Dictionary<string, string>() { { "percent", $"{moviesMigrationPercent}" } }
                        }
                    }
                })
                :
                CreateCluster("movies", LoadBalancingPolicies.RoundRobin, new Dictionary<string, DestinationConfig>()
                {
                    {
                        "destination1",
                        new DestinationConfig()
                        {
                            Address = monolithUrl
                        }
                    }
                });

            var clusters = new[]
            {
                moivesCluster,
                CreateCluster("events", LoadBalancingPolicies.RoundRobin, new Dictionary<string, DestinationConfig>()
                {
                    {
                        "destination1",
                        new DestinationConfig()
                        {
                            Address = eventsUrl
                        }
                    }
                }),
                CreateCluster("others", LoadBalancingPolicies.RoundRobin, new Dictionary<string, DestinationConfig>()
                {
                    {
                        "destination1",
                        new DestinationConfig()
                        {
                            Address = monolithUrl
                        }
                    },
                }),
            };

            return (routes, clusters);
        }

        public sealed class ProcentLoadBalancingPolicy : ILoadBalancingPolicy
        {
            public string Name => "percent";

            public DestinationState? PickDestination(HttpContext context, ClusterState cluster, IReadOnlyList<DestinationState> availableDestinations)
            {
                var sum = availableDestinations.Sum(d => d.ConcurrentRequestCount);

                if (sum == 0)
                {
                    var firstDestination = availableDestinations[0];
                    firstDestination.ConcurrentRequestCount++;

                    return firstDestination;
                }

                foreach (var destination in availableDestinations)
                {
                    var requestCount = destination.ConcurrentRequestCount;
                    var percent = int.TryParse(destination.Model.Config.Metadata["percent"], out int parsedPersent) ? parsedPersent : 0;

                    if ((requestCount * 100 / sum) < percent)
                    {
                        destination.ConcurrentRequestCount++;

                        return destination;
                    }
                }

                var anyDestination = availableDestinations[0];
                anyDestination.ConcurrentRequestCount++;

                return anyDestination;
            }
        }

        private static ClusterConfig CreateCluster(string name, string loadBalancingPolicyName, Dictionary<string, DestinationConfig> destinations)
        {
            return new ClusterConfig()
            {
                ClusterId = $"{name}-cluster",
                LoadBalancingPolicy = loadBalancingPolicyName,
                Destinations = destinations,
                HttpClient = new HttpClientConfig
                {
                    MaxConnectionsPerServer = 100,
                    SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13
                }
            };
        }

        private static RouteConfig CreatedRoute(string name, string path)
        {
            return new RouteConfig()
            {
                RouteId = $"{name}-route",
                ClusterId = $"{name}-cluster",
                Match = new RouteMatch
                {
                    Path = path
                }
            };
        }
    }
}
