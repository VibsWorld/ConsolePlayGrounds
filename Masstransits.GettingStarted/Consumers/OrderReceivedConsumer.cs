namespace Masstransits.Setup.Consumers;

using System;
using System.Threading.Tasks;
using MassTransit;
using Masstransits.Setup.Contracts;
using Microsoft.Extensions.Logging;

public class OrderReceivedConsumer : IConsumer<OrderReceived>
{
    readonly ILogger<OrderReceivedConsumer> _logger;

    public OrderReceivedConsumer() { }

    public OrderReceivedConsumer(ILogger<OrderReceivedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderReceived> context)
    {
        _logger.LogInformation(
            "Received Text from Inbuilt Consumer: {Text}",
            context.Message.Value
        );

        var orderProcessed = new OrderProcessed(
            context.Message.Value + "_" + Guid.NewGuid().ToString()
        );

        await context.Send(orderProcessed);
        await Task.Delay(1);
    }
}
