using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Valuts.Azure.Console1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var secretClient = new SecretClient(
                new Uri("https://pvprod-keyvault.vault.azure.net"),
                new DefaultAzureCredential(),
                new SecretClientOptions()
            );

            var response = secretClient.GetSecret("redis-connectionstring");
            var value = response?.Value?.Value ?? throw new CredentialUnavailableException("");
            Console.WriteLine($"redis-connectionstring - {value}");

            response = secretClient.GetSecret("application-rabbitmq-host");
            value = response?.Value?.Value ?? throw new CredentialUnavailableException("");
            Console.WriteLine($"redis-connectionstring - {value}");
        }
    }
}
