namespace Masstransits.Setup.Consumers;

using System.Threading.Tasks;
using MassTransit;
using Masstransits.Setup.Contracts;
using Microsoft.Extensions.Logging;

public class HelloWorldContractConsumer : IConsumer<HelloWorldContract>
{
    readonly ILogger<HelloWorldContractConsumer> _logger;

    public HelloWorldContractConsumer() { }

    public HelloWorldContractConsumer(ILogger<HelloWorldContractConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<HelloWorldContract> context)
    {
        _logger.LogInformation(
            "Received Text from Inbuilt Consumer: {Text}",
            context.Message.Value
        );
        await Task.Delay(1);
    }
}
