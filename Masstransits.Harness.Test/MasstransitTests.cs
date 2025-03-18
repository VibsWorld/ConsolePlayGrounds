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
        var consumerHarness = harness.Consumer<OrderReceivedConsumer>();

        await harness.Start();
        try
        {
            var message = new OrderReceived
            {
                Value = $"Order received with Order Id 1 at {DateTime.Now}"
            };
            await harness.InputQueueSendEndpoint.Send(message);
            Assert.True(await consumerHarness.Consumed.Any<OrderReceived>());
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
                x.AddConsumer<OrderReceivedConsumer>();

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
            var message = new OrderReceived
            {
                Value = $"Order received with Order Id 1 at {DateTime.Now}"
            };

            await harness.Bus.Publish(message);

            Assert.True(await harness.Published.Any<OrderReceived>());

            Assert.True(await harness.Consumed.Any<OrderReceived>());

            Assert.True(await harness.Sent.Any<OrderProcessed>());

            var consumerHarness = harness.GetConsumerHarness<OrderReceivedConsumer>();

            Assert.True(await consumerHarness.Consumed.Any<OrderReceived>());

            var subConsumerHarness = harness.GetConsumerHarness<OrderProcessedConsumer>();

            Assert.True(await consumerHarness.Consumed.Any<OrderProcessed>());
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
