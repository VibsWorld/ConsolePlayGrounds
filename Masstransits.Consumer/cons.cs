using MassTransit;
using Masstransits.Setup.Contracts;

namespace Masstransits.Consumer;

public class HelloWorldContractConsumer : IConsumer<HelloWorldContract>
{
    readonly ILogger<HelloWorldContractConsumer> _logger;

    public HelloWorldContractConsumer(ILogger<HelloWorldContractConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<HelloWorldContract> context)
    {
        _logger.LogInformation(
            "Received Text from External Consumer: {Text}",
            context.Message.Value
        );
        await Task.Delay(1);
    }
}
