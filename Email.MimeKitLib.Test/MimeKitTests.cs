using System.Collections.Concurrent;
using Bogus;
using Common.Fixtures.Email;
using FluentAssertions;
using MailKit.Security;
using MimeKit;
using Xunit.Abstractions;

namespace Email.MimeKitLib.Test;

public class MimeKitTests : IAsyncLifetime
{
    private static ConcurrentBag<KeyValuePair<string, bool>> messages = new();
    private readonly ITestOutputHelper testOutputHelper;
    private SmtpFixture smtpFixture = new();

    public MimeKitTests(ITestOutputHelper testOutputHelper) =>
        this.testOutputHelper = testOutputHelper;

    public async Task InitializeAsync()
    {
        smtpFixture = new SmtpFixture();
        await smtpFixture!.StartAsync();
    }

    [Fact]
    public async Task MailSmtp_SendsMail_EmailCountShouldMatch()
    {
        int totalCount = 10;

        var faker = new Faker("en");

        var host = "localhost";
        ushort port = smtpFixture!.PortSmtp;
        var userName = faker.Person.UserName;
        var password = faker.Person.Random.AlphaNumeric(8);

        await Parallel.ForAsync(
            0,
            totalCount,
            async (i, _) =>
            {
                using var message = new MimeMessage();
                message.From.Add(new MailboxAddress(faker.Person.FullName, faker.Person.Email));
                message.To.Add(new MailboxAddress(faker.Person.FullName, faker.Person.Email));
                message.Subject = faker.Lorem.Sentence();
                message.Body = new TextPart("plain") { Text = faker.Lorem.Sentences(5) };

                await ProcessEmails(message, host, port, userName, password);
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

    public async Task DisposeAsync()
    {
        if (smtpFixture is not null)
            await smtpFixture.DisposeAsync().AsTask();
    }

    private static async Task ProcessEmails(
        MimeMessage mimeMessage,
        string host,
        ushort port,
        string username,
        string password
    )
    {
        var messageId = Guid.NewGuid();
        try
        {
            using var client = new MailKit.Net.Smtp.SmtpClient();
            client.Connect(host, port, SecureSocketOptions.None);
            client.Authenticate(username, password);
            var result = await client.SendAsync(mimeMessage);
            messages.Add(new KeyValuePair<string, bool>($"{messageId}", true));
            client.Disconnect(true);
        }
        catch
        {
            messages.Add(new KeyValuePair<string, bool>($"{messageId}", false));
        }
    }
}
