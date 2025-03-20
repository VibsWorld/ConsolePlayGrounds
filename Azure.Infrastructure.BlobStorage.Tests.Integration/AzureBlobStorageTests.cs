using AutoFixture;
using Azure.BlobStorage.Infrastructure;
using FluentAssertions;

namespace Azure.Infrastructure.BlobStorage.Tests.Integration
{
    public class AzureBlobStorageTests : IClassFixture<AzureBlobStorageFixture>
    {
        private readonly string azureConnectionString;
        private readonly AzureBlobUploader azureBlobUploader;
        private readonly AzureBlobDownloader azureBlobDownloader;
        private readonly int azureBlobStoragePort;

        public AzureBlobStorageTests(AzureBlobStorageFixture azureBlobStorageFixture)
        {
            azureBlobStoragePort = azureBlobStorageFixture.Port;
            azureConnectionString = BuildLocalStorageAccountConnectionString();
            azureBlobUploader = new AzureBlobUploader(azureConnectionString);
            azureBlobDownloader = new AzureBlobDownloader(azureConnectionString);
        }

        [Fact]
        public async Task AzureBlobUploader_UploadFileToAzureBlobStorage_ShouldDownloadFileWithSameFileContentsInclusiveOfSpecialCharactersAndLineBreaks()
        {
            BuildTestFixture(out string containerName, out string fileName, out string fileContent);
            await azureBlobUploader.Upload(containerName, fileName, fileContent);
            await Task.Delay(300);

            var downloadedResult = await azureBlobDownloader.GetUriWithDataAsync(
                containerName,
                fileName
            );

            downloadedResult.data.Should().Be(fileContent);
            downloadedResult
                .uri.Should()
                .Be(
                    BuildLocalAccountAzureBlobStorageUrlFromContainerNameAndFileName(
                        containerName,
                        fileName
                    )
                );
        }

        private static void BuildTestFixture(
            out string containerName,
            out string fileName,
            out string fileContent
        )
        {
            containerName = new Fixture().Create<string>();
            fileName = new Fixture().Create<string>();
            fileContent =
                new Fixture().Create<string>()
                + Environment.NewLine
                + "\t"
                + new Fixture().Create<string>()
                + Environment.NewLine
                + new Fixture().Create<string>();
        }

        private string BuildLocalAccountAzureBlobStorageUrlFromContainerNameAndFileName(
            string containerName,
            string fileName
        ) => $"http://127.0.0.1:{azureBlobStoragePort}/devstoreaccount1/{containerName}/{fileName}";

        private string BuildLocalStorageAccountConnectionString() =>
            $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:{azureBlobStoragePort}/devstoreaccount1;";
    }
}
