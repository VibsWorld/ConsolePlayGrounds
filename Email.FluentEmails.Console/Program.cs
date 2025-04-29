using System.Net;
using System.Net.Mail;
using Common.Fixtures.Email;
using FluentEmail.Core;
using Microsoft.Extensions.DependencyInjection;

//https://github.com/rnwood/smtp4dev/blob/master/docker-compose.yml
namespace Email.FluentEmails.ConsoleTests;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var smtpFixture = new SmtpFixture();
        smtpFixture.StartAsync().GetAwaiter().GetResult();
        var services = new ServiceCollection();

        var defaultFromEmail = "test@test.com";
        var host = "localhost";
        var port = smtpFixture.PortSmtp;
        var userName = "test";
        var password = "test";
        //services.AddFluentEmail(defaultFromEmail).AddSmtpSender(host, port, userName, password);
        var smtpClient = new SmtpClient(host, port)
        {
            EnableSsl = false,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(userName, password),
        };
        services.AddFluentEmail(defaultFromEmail).AddSmtpSender(smtpClient);
        var provider = services.BuildServiceProvider();

        Parallel.For(
            1,
            10,
            (i) =>
            {
                var _fluentEmail = provider.GetService<IFluentEmail>();

                var response = _fluentEmail!
                    .To($"test{i}@test.com")
                    .Subject("test suject")
                    .Body("test body")
                    .Send();

                if (!response.Successful)
                    Console.WriteLine(response.MessageId);
            }
        );
    }
}
