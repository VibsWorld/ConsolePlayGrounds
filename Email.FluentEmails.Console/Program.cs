using FluentEmail.Core;
using Microsoft.Extensions.DependencyInjection;

//https://github.com/rnwood/smtp4dev/blob/master/docker-compose.yml
namespace Email.FluentEmails.ConsoleTests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var services = new ServiceCollection();

            var defaultFromEmail = "test@test.com";
            var host = "localhost";
            var port = 25;
            //var userName = "test";
            //var password = "test";
            services.AddFluentEmail(defaultFromEmail).AddSmtpSender(host, port);
            var provider = services.BuildServiceProvider();

            var _fluentEmail = provider.GetService<IFluentEmail>();

            var response = _fluentEmail!
                .To("test1@test.com")
                .Subject("test suject")
                .Body("test body")
                .Send();
        }
    }
}
