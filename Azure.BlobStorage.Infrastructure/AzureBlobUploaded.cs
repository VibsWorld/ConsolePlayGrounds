using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Azure.BlobStorage.Infrastructure;

public class AzureBlobUploader
{
    private readonly string labelBlobConnectionString;

    public AzureBlobUploader(string labelBlobConnectionString)
    {
        this.labelBlobConnectionString = labelBlobConnectionString;
    }

    public async Task<Uri> Upload(string containerName, string blobFileName, string data)
    {
        var blobServiceClient = new BlobServiceClient(labelBlobConnectionString);

        var containers = blobServiceClient.GetBlobContainers();

        var containerClient = containers.All(x => x.Name != containerName)
            ? await blobServiceClient.CreateBlobContainerAsync(containerName, PublicAccessType.Blob)
            : blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobFileName);
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));

        await blobClient.UploadAsync(memoryStream, true);

        return blobClient.Uri;
    }
}

public class AzureBlobDownloader
{
    private readonly string labelBlobConnectionString;

    public AzureBlobDownloader(string labelBlobConnectionString)
    {
        this.labelBlobConnectionString = labelBlobConnectionString;
    }

    public async Task<(Uri uri, string data)> GetUriWithDataAsync(
        string containerName,
        string blobFileName
    )
    {
        var blobServiceClient = new BlobServiceClient(labelBlobConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobFileName);
        var res = await blobClient.DownloadContentAsync();
        return (blobClient.Uri, Encoding.UTF8.GetString(res.Value.Content));
    }
}
