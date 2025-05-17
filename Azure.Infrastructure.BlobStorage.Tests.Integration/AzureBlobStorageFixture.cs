using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Azure.Infrastructure.BlobStorage.Tests.Integration;

public class AzureBlobStorageFixture : IAsyncLifetime
{
    private IContainer? azureBlobStorageContainer;
    private INetwork? containerNetwork;
    public const int AzureBlobStorageHostPort = 10000;

    public int Port { get; private set; }

    public async Task InitializeAsync()
    {
        containerNetwork = new NetworkBuilder().Build();
        azureBlobStorageContainer = InitializeAzureBlbStorageContainer();
        await azureBlobStorageContainer.StartAsync();
        Port = azureBlobStorageContainer.GetMappedPublicPort(AzureBlobStorageHostPort);
    }

    public async Task DisposeAsync()
    {
        if (azureBlobStorageContainer is not null)
            await azureBlobStorageContainer!.DisposeAsync();
    }

    private IContainer InitializeAzureBlbStorageContainer() =>
        new ContainerBuilder()
            .WithNetwork(containerNetwork)
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithCommand("azurite-blob")
            .WithCommand("--blobHost", "0.0.0.0")
            .WithPortBinding(AzureBlobStorageHostPort, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilPortIsAvailable(AzureBlobStorageHostPort)
            )
            .Build();
}
