using System.Collections.Concurrent;
using Common.Fixtures.Email;
using MailKit.Security;
using MimeKit;

namespace Email.MailKitSample.ConsoleTests;

public class Program
{
    private static ConcurrentBag<KeyValuePair<string, bool>> messages = new();

    static async Task Main(string[] args)
    {
        var smtpFixture = new SmtpFixture();
        await smtpFixture.StartAsync();

        var defaultFromEmail = "FromTest@test.com";
        var defaultToEmail = "ToTest@test.com";
        var host = "localhost";
        ushort port = smtpFixture.PortSmtp;
        var userName = "test";
        var password = "test";

        Console.WriteLine("Hello, World!");

        await Parallel.ForAsync(
            1,
            10,
            async (i, _) =>
            {
                using var message = new MimeMessage();
                message.From.Add(new MailboxAddress("FromName", defaultFromEmail));
                message.To.Add(new MailboxAddress("", defaultToEmail));
                message.Subject = $"Test Subject({i}) - {DateTime.Now}";
                message.Body = new TextPart("plain") { Text = $"Hello Message body - #{i}" };

                await ProcessEmails(message, host, port, userName, password);
            }
        );

        foreach (var message in messages)
        {
            Console.WriteLine($"{message}");
        }
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
