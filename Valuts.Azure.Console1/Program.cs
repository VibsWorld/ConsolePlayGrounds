using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Vault.Azure.Utils;

namespace Valuts.Azure.Console1
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var secretClient = new SecretClient(
                new Uri("https://pvprod-keyvault.vault.azure.net"),
                new DefaultAzureCredential(),
                new SecretClientOptions()
            );

            var response = await secretClient.GetSecretAsync("redis-connectionstring");

            var azureClient = new AzureClient("https://pvprod-keyvault.vault.azure.net");
            var redisConnectionString = await azureClient.GetSecret("redis-connectionstring");
            Console.WriteLine(
                $"redis-connectionstring - {await azureClient.GetSecret("redis-connectionstring")}"
            );
            Console.WriteLine(
                $"application-rabbitmq-host - {await azureClient.GetSecret("application-rabbitmq-host")}"
            );
        }

        /*
        using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace key_vault_console_app
{
    class Program
    {
        static async Task Main(string[] args)
        {
            const string secretName = "SECRET_NAME";
            var keyVaultName = "KV_NAME"
            var kvUri = $"https://{keyVaultName}.vault.azure.net";

            var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

            var secretValue = "SECRET_VALUE"

            Console.Write($"Creating a secret in {keyVaultName} called '{secretName}' with the value '{secretValue}' ...");
            await client.SetSecretAsync(secretName, secretValue);
            Console.WriteLine(" done.");

            Console.WriteLine("Forgetting your secret.");
            secretValue = string.Empty;
            Console.WriteLine($"Your secret is '{secretValue}'.");

            Console.WriteLine($"Retrieving your secret from {keyVaultName}.");
            var secret = await client.GetSecretAsync(secretName);
            Console.WriteLine($"Your secret is '{secret.Value.Value}'.");

            Console.Write($"Deleting your secret from {keyVaultName} ...");
            DeleteSecretOperation operation = await client.StartDeleteSecretAsync(secretName);
            // You only need to wait for completion if you want to purge or recover the secret.
            await operation.WaitForCompletionAsync();
            Console.WriteLine(" done.");

            Console.Write($"Purging your secret from {keyVaultName} ...");
            await client.PurgeDeletedSecretAsync(secretName);
            Console.WriteLine(" done.");
        }
    }
}

         */
    }
}
