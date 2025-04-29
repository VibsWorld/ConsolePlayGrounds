using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using Bogus;
using Common.Fixtures.Email;
using FluentAssertions;
using FluentEmail.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Email.FluentMailLib.Test;

public class FluentEmailTests : IAsyncLifetime
{
    private static ConcurrentBag<KeyValuePair<string, bool>> messages = new();
    private readonly ITestOutputHelper testOutputHelper;
    private SmtpFixture smtpFixture = new();
    private readonly Faker faker = new Faker("en");

    public FluentEmailTests(ITestOutputHelper testOutputHelper) =>
        this.testOutputHelper = testOutputHelper;

    public async Task InitializeAsync()
    {
        smtpFixture = new SmtpFixture();
        await smtpFixture!.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (smtpFixture is not null)
            await smtpFixture.DisposeAsync().AsTask();

        messages.Clear();
    }

    [Fact]
    public async Task SendEmail_ShouldSendEmail_EmailCountShouldMatch()
    {
        int totalCount = 10;

        await Parallel.ForAsync(
            0,
            10,
            async (i, _) =>
            {
                var provider = BuildServiceProviderForFluentEmail(
                    smtpFixture.PortSmtp,
                    "localhost",
                    faker.Internet.UserName(),
                    faker.Internet.Password(),
                    faker.Person.Email
                );

                var fluentEmailFactory = provider.GetService<IFluentEmailFactory>();

                var response = await fluentEmailFactory!
                    .Create()
                    .To(faker.Person.Email)
                    .Subject(faker.Lorem.Sentence())
                    .Body(faker.Lorem.Sentences(20))
                    .SendAsync();

                if (response is not null && response.Successful)
                    messages.Add(
                        new KeyValuePair<string, bool>(
                            Guid.NewGuid().ToString(),
                            response.Successful
                        )
                    );
            }
        );

        foreach (var message in messages)
            testOutputHelper.WriteLine(message.ToString());

        messages.Count.Should().Be(totalCount);
        messages
            .Should()
            .AllSatisfy(x =>
            {
                x.Value.Should().Be(true);
            });
    }

    private ServiceProvider BuildServiceProviderForFluentEmail(
        ushort port,
        string host,
        string userName,
        string password,
        string? fromEmail = null
    )
    {
        var services = new ServiceCollection();

        var smtpClient = new SmtpClient(host, port)
        {
            EnableSsl = false,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(userName, password),
        };

        services.AddFluentEmail(fromEmail ?? faker.Person.Email).AddSmtpSender(smtpClient);

        return services.BuildServiceProvider();
    }
}
