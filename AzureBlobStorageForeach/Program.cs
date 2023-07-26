using Microsoft.Extensions.Configuration;

namespace AzureBlobStorageForeach
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                //.AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .Build();

            var connectionString = config.GetConnectionString("BlobStorage");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("BlobStorage connection string is empty.");
            }

            // Replace "your_connection_string" with your actual Azure Blob Storage connection string
            var blobStorageClient = new BlobStorageClient(connectionString);

            Console.WriteLine("Starting properties update...");

            IEnumerable<string> containerNames = await blobStorageClient.ListContainersAsync();
            foreach (var containerName in containerNames)
            {
                var blobs = await blobStorageClient.ListBlobsInContainerAsync(containerName);
                foreach (var blob in blobs)
                {
                    await blobStorageClient.SetBlobPropertiesAsync(containerName, blob);
                }
            }

            Console.WriteLine("Process finished.");
        }
    }
}