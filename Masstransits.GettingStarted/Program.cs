using System.Reflection;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MassTransit;
using Masstransits.Setup;
using Masstransits.Setup.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

//https://github.com/MassTransit/Sample-GettingStarted/tree/master#install-rabbitmq
namespace Masstransits
{
    public class Program
    {
        private const string VirtualHost = "ParcelVision.Retail";
        private const string Username = "pvRetailDev";
        private const string Password = "pvRetailDev";

        private const ushort RabbitMqHostPort = 5672;
        private const ushort RabbitMqHttpManagementPort = 15672;
        private static ushort ContainerPort;
        private static INetwork dockerNetwork = new NetworkBuilder().Build();
        private static IContainer containerRabbitMq;

            

        public static async Task Main(string[] args)
        {
            InitializeRabbitmqContainerOptions();
            await containerRabbitMq.StartAsync();
            ContainerPort = containerRabbitMq.GetMappedPublicPort(RabbitMqHostPort);
            var build = CreateHostBuilder(args).Build();

            await build.RunAsync();
        }

        private static void InitializeRabbitmqContainerOptions() =>
        containerRabbitMq = new ContainerBuilder()
            .WithNetwork(dockerNetwork)
            .WithImage("rabbitmq:3-management")
            .WithEnvironment("RABBITMQ_DEFAULT_USER", Username)
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", Password)
            .WithEnvironment("RABBITMQ_DEFAULT_VHOST", VirtualHost)
            .WithPortBinding(RabbitMqHostPort, RabbitMqHostPort)
            .WithPortBinding(RabbitMqHttpManagementPort, RabbitMqHttpManagementPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(RabbitMqHostPort))
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilPortIsAvailable(RabbitMqHttpManagementPort)
            )
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request =>
                        request.ForPort(RabbitMqHttpManagementPort).ForPath("/")
                    )
            )
            .Build();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services.AddMassTransit(x =>
                        {
                            
                            //x.AddConsumer<HelloWorldContractConsumer>();
                            x.AddConsumers(Assembly.GetExecutingAssembly());
                            x.UsingRabbitMq(
                                (context, cfg) =>
                                {
                                    cfg.Host(
                                        "localhost",
                                        ContainerPort,
                                        VirtualHost,
                                        h =>
                                        {
                                            h.Username(Username);
                                            h.Password(Password);
                                        }
                                    );

                                    cfg.ConfigureEndpoints(context);
                                }
                            );
                        });

                        services.AddHostedService<Worker>();
                    }
                );
    }
}
