/*
 *  https://github.com/MassTransit/Sample-WebApplicationFactory
 *  https://masstransit.io/documentation/concepts/testing
 */

using DotNet.Testcontainers.Builders;
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
    private const int RabbitMqManagenentPort = 15672;
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
            var result = await harness.Sent.Any<OrderProcessed>();
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task TestInRabbitMqLiveTestharness()
    {
        var container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithPortBinding(ContainerPort, RabbitMqHostPort)
            .WithPortBinding(RabbitMqManagenentPort, RabbitMqManagenentPort) // Management UI port
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request =>
                        request.ForPort(RabbitMqManagenentPort).ForPath("/")
                    )
            )
            .Build();
        await container.StartAsync();

        await using var provider = new ServiceCollection()
            //.AddYourBusinessServices() // register all of your normal business services
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<OrderReceivedConsumer>();
                x.AddConsumer<OrderProcessedConsumer>();

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

        EndpointConvention.Map<OrderProcessed>(new Uri("queue:order-received"));

        await harness.Start();

        try
        {
            var message = new OrderReceived
            {
                Value = $"Order received with Order Id 1 at {DateTime.Now}"
            };

            await harness.Bus.Publish(message);
            //await harness.Bus.Send(message);

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
