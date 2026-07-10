using System;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace KeyVaultSoftDeleteDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Replace with your Key Vault URL
            string keyVaultUrl = "https://<your-key-vault-name>.vault.azure.net/";

            // Create a SecretClient using DefaultAzureCredential
            var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            string secretName = "demoSecret";
            string secretValue = "SuperSecretValue123";

            try
            {
                // 1️⃣ Create a secret
                Console.WriteLine($"Creating secret '{secretName}'...");
                await client.SetSecretAsync(secretName, secretValue);
                Console.WriteLine("Secret created successfully.\n");

                // 2️⃣ Soft delete the secret
                Console.WriteLine($"Soft deleting secret '{secretName}'...");
                DeleteSecretOperation deleteOp = await client.StartDeleteSecretAsync(secretName);
                await deleteOp.WaitForCompletionAsync();
                Console.WriteLine("Secret soft deleted.\n");

                // 3️⃣ List deleted secrets
                Console.WriteLine("Listing deleted secrets:");
                await foreach (DeletedSecret deleted in client.GetDeletedSecretsAsync())
                {
                    Console.WriteLine($"- {deleted.Name} (Deleted on: {deleted.Properties.DeletedOn})");
                }
                Console.WriteLine();

                // 4️⃣ Recover the deleted secret
                Console.WriteLine($"Recovering secret '{secretName}'...");
                RecoverDeletedSecretOperation recoverOp = await client.StartRecoverDeletedSecretAsync(secretName);
                await recoverOp.WaitForCompletionAsync();
                Console.WriteLine("Secret recovered successfully.\n");

                // Verify recovery
                KeyVaultSecret recoveredSecret = await client.GetSecretAsync(secretName);
                Console.WriteLine($"Recovered secret value: {recoveredSecret.Value}");
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Azure request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }
    }
}
