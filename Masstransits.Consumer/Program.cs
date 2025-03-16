using System.Reflection;
using MassTransit;

namespace Masstransits.Consumer
{
    internal class Program
    {
        /// <summary>
        /// Independed consumer can run individually
        /// </summary>
        private const string VirtualHost = "ParcelVision.Retail";
        private const string Username = "pvRetailDev";
        private const string Password = "pvRetailDev";

        private const ushort RabbitMqHostPort = 5672;
        private const ushort RabbitMqHttpManagementPort = 15672;
        private static ushort ContainerPort = RabbitMqHostPort;

        static void Main(string[] args)
        {
            Console.WriteLine("Consumer strated!");
            var build = CreateHostBuilder(args).Build();
            build.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft
                .Extensions.Hosting.Host.CreateDefaultBuilder(args)
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
                    }
                );
    }
}
