using System.Threading.Tasks;
using Docker.DotNet.Models;
using MassTransit;
using Masstransits.Setup;
using Masstransits.Setup.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.RabbitMq;

//https://github.com/MassTransit/Sample-GettingStarted/tree/master#install-rabbitmq
namespace Masstransits
{
    public class Program
    {
        private const string VirtualHost = "ParcelVision.Retail";
        private const string Username = "pvRetailDev";
        private const string Password = "pvRetailDev";
        private static ushort ContainerPort;
        private static RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithHostname(VirtualHost)
            .WithUsername(Username)
            .WithPassword(Password)
            .WithPortBinding(RabbitMqBuilder.RabbitMqPort, false)
            //.WithExposedPort(RabbitMqBuilder.RabbitMqPort)
            .Build();

        public static async Task Main(string[] args)
        {
            await _rabbitMqContainer.StartAsync();
            ContainerPort = _rabbitMqContainer.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort);
            var build = CreateHostBuilder(args).Build();

            await build.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services.AddMassTransit(x =>
                        {
                            // elided...
                            x.AddConsumer<HelloWorldContractConsumer>();
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
