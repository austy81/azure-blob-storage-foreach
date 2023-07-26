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
            
            var blobStorageClient = GetBlobStorageClient(config);
            
            var sqlClient = GetSqlClient(config);
            var attachments = sqlClient.ReadAttachements();

            Console.WriteLine("Starting properties update...");

            IEnumerable<string> containerNames = await blobStorageClient.ListContainersAsync();
            foreach (var containerName in containerNames.Where(c => c.EndsWith("-attachments")))
            {
                var blobs = await blobStorageClient.ListBlobsInContainerAsync(containerName);
                foreach (var blob in blobs.Skip(300))
                {
                    string targetFilename = LookupFilenameByBlobName(attachments, blob);
                    await blobStorageClient.SetBlobPropertiesAsync(containerName, blob, targetFilename);
                }
            }

            Console.WriteLine("Process finished.");
        }

        private static string LookupFilenameByBlobName(IEnumerable<AttachmentDTO> attachments, string blob)
        {
            var correspondingAttachmentRecord = attachments.FirstOrDefault(a => a.AzureStorageBlobName == blob);
            var targetFilename = correspondingAttachmentRecord?.TargetFilename ?? string.Empty;
            return targetFilename;
        }

        private static BlobStorageClient GetBlobStorageClient(IConfigurationRoot config)
        {
            var connectionString = config.GetConnectionString("BlobStorageProd");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("BlobStorage connection string is empty.");
            }

            var blobStorageClient = new BlobStorageClient(connectionString);
            return blobStorageClient;
        }

        private static SqlClient GetSqlClient(IConfigurationRoot config)
        {
            var connectionString = config.GetConnectionString("SQLServerProd");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("BlobStorage connection string is empty.");
            }

            return new SqlClient(connectionString);
        }
    }
}