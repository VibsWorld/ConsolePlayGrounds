/*
 *  https://github.com/MassTransit/Sample-WebApplicationFactory
 *  https://masstransit.io/documentation/concepts/testing
 */

using MassTransit;
using MassTransit.Testing;
using Masstransits.Setup.Consumers;
using Masstransits.Setup.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.RabbitMq;

namespace Masstransits.Harness.Test;

public class MasstransitTests
{
    private const ushort RabbitMqHostPort = 5672;

    private static readonly ushort ContainerPort = RabbitMqHostPort;

    [Fact]
    public async Task TestInMemoryTestharness()
    {
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer<HelloWorldContractConsumer>();

        await harness.Start();
        try
        {
            var message = new HelloWorldContract { Value = "Hello, MassTransit!" };
            await harness.InputQueueSendEndpoint.Send(message);
            Assert.True(await consumerHarness.Consumed.Any<HelloWorldContract>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task TestInRabbitMqLiveTestharness()
    {
        var container = new RabbitMqBuilder().Build();
        await container.StartAsync();

        await using var provider = new ServiceCollection()
            //.AddYourBusinessServices() // register all of your normal business services
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<HelloWorldContractConsumer>();

                x.UsingRabbitMq(
                    (context, cfg) =>
                    {
                        cfg.Host(
                            "localhost",
                            container.GetMappedPublicPort(ContainerPort),
                            "/",
                            h =>
                            {
                                h.Username(RabbitMqBuilder.DefaultUsername);
                                h.Password(RabbitMqBuilder.DefaultPassword);
                            }
                        );

                        cfg.ConfigureEndpoints(context);
                    }
                );
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();

        try
        {
            var message = new HelloWorldContract { Value = "Hello, MassTransit!" };

            await harness.Bus.Publish(
                new HelloWorldContract { Value = "hellow world - " + DateTime.Now.ToString() }
            );

            Assert.True(await harness.Published.Any<HelloWorldContract>());

            Assert.True(await harness.Consumed.Any<HelloWorldContract>());

            //var consumerHarness = harness.GetConsumerHarness<HelloWorldContractConsumer>();

            // Assert.True(await consumerHarness.Consumed.Any<HelloWorldContract>());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
