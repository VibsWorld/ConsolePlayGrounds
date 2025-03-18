namespace Masstransits.Setup.Consumers;

using System.Threading.Tasks;
using MassTransit;
using Masstransits.Setup.Contracts;
using Microsoft.Extensions.Logging;

public class OrderProcessedConsumer : IConsumer<OrderProcessed>
{
    readonly ILogger<OrderProcessedConsumer> _logger;

    public OrderProcessedConsumer() { }

    public OrderProcessedConsumer(ILogger<OrderProcessedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderProcessed> context)
    {
        _logger.LogInformation(
            "Received Text from Inbuilt Consumer: {Text}",
            context.Message.SubValue
        );
        await Task.Delay(1);
    }
}
