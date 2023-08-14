using AzureBlobStorageForeach.DTOs;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.Json;

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

            //await RunBlobContainerUpdate(config);

            var sqlClient = GetSqlClient(config);
            AuditCustomData(config, await sqlClient.ReadCustomDataTenant("Orders"), "Orders");
            AuditCustomData(config, await sqlClient.ReadCustomDataTenant("ServiceObjects"), "ServiceObjects");
            AuditCustomData(config, await sqlClient.ReadCustomDataTenant("Customers"), "Customers");
        }

        private static void AuditCustomData(IConfigurationRoot config, IEnumerable<CustomDataTenant>? entityList, string auditedObject)
        {
            if (entityList == null)
            {
                return;
            }

            var tenantErrors = new List<string>();
            Console.WriteLine($"Processing object: {auditedObject}. Total rows: {entityList.Count()}");
            foreach (var order in entityList)
            {
                if (!string.IsNullOrEmpty(order.CustomDataString))
                {
                    try
                    {
                        //var cleanedCustomDataString = Utils.SanitizeJsonString(order.CustomDataString);
                        var cleanedCustomDataString = order.CustomDataString;
                        var customDataObject = JsonDocument.Parse(cleanedCustomDataString);
                    }
                    catch (Exception e)
                    {
                        tenantErrors.Add($"{order.TenantCode} - {e.Message}");
                    }
                }
            }
            var errors = tenantErrors.Count();
            Console.WriteLine($"Total errors: {errors}");
            var distinctTenants = tenantErrors.Distinct().ToList();
            foreach ( var tenant in distinctTenants)
                Console.WriteLine($"{auditedObject} tenants with error: {tenant}");
        }

        private static async Task RunBlobContainerUpdate(IConfigurationRoot config)
        {
            var blobStorageClient = GetBlobStorageClient(config);

            var sqlClient = GetSqlClient(config);
            var attachments = await sqlClient.ReadAttachements();

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
        }

        private static string LookupFilenameByBlobName(IEnumerable<Attachment> attachments, string blob)
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
            //var connectionString = config.GetConnectionString("SQLServer");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("SQLServer connection string is empty.");
            }

            return new SqlClient(connectionString);
        }
    }
}