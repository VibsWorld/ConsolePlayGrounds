using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Masstransits.Setup.Contracts;
using Microsoft.Extensions.Hosting;

namespace Masstransits.Setup;

public class Worker : BackgroundService
{
    readonly IBus _bus;

    public Worker(IBus bus)
    {
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _bus.Publish(
                new OrderReceived { Value = $"The time is {DateTimeOffset.Now}" },
                stoppingToken
            );

            await Task.Delay(1000, stoppingToken);
        }
    }
}
