using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Vault.Azure.Utils
{
    public class AzureClient
    {
        private readonly SecretClient secretClient;

        public AzureClient(SecretClient secretClient)
        {
            this.secretClient = secretClient;
        }

        public AzureClient(
            string url,
            DefaultAzureCredential? defaultAzureCredential = null,
            SecretClientOptions? secretClientOptions = null
        )
        {
            secretClient = new SecretClient(
                new Uri(url),
                defaultAzureCredential ?? new DefaultAzureCredential(),
                secretClientOptions ?? new SecretClientOptions()
            );
        }

        public async Task<(bool isSuccessful, string? secret)> GetSecret(string key)
        {
            var keyVaultSecret = await secretClient.GetSecretAsync(key);
            var secret = keyVaultSecret?.Value?.Value;
            return secret is null ? (false, null) : (true, secret);
        }
    }
}
