//SELECT sum(SizeBytes)/1000000000 as SizeGB, count(*) NumberOfFiles, TenantCode
//FROM [dbo].[Attachments] a
//join [dbo].Companies c on a.CompanyId = c.Id
//group by TenantCode
//order by sum(SizeBytes) desc

//SELECT 
//    SUM(CASE WHEN SizeBytes > 0 THEN 1 ELSE 0 END) AS CountGreaterThanZero,
//    SUM(CASE WHEN SizeBytes = 0 THEN 1 ELSE 0 END) AS CountEqualsZero,
//    COUNT(*) AS TotalRecords,
//    100.0 * SUM(CASE WHEN SizeBytes > 0 THEN 1 ELSE 0 END) / COUNT(*) AS PercentageGreaterThanZero,
//    100.0 * SUM(CASE WHEN SizeBytes = 0 THEN 1 ELSE 0 END) / COUNT(*) AS PercentageEqualsZero
//FROM Attachments;


using AzureBlobStorageForeach.DTOs;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AzureBlobStorageForeach
{
    internal class Program
    {
        private const string attachmentsContainerSuffix = "-attachments";

        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                //.AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .Build();

            await PrintNotExistingDBAttachments(config);

            //await UpdateAttachmentsSize(config);

            //await RunBlobContainerUpdate(config);

            //var sqlClient = GetSqlClient(config);
            //AuditCustomData(config, await sqlClient.ReadCustomDataTenant("Orders"), "Orders");
            //AuditCustomData(config, await sqlClient.ReadCustomDataTenant("ServiceObjects"), "ServiceObjects");
            //AuditCustomData(config, await sqlClient.ReadCustomDataTenant("Customers"), "Customers");
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

        private static async Task UpdateAttachmentsSize(IConfigurationRoot config, string processTenant = "schlieger")
        {
            var blobStorageClient = GetBlobStorageClient(config);

            IEnumerable<string> containerNames = await blobStorageClient.ListContainersAsync();
            List<string> attachmentContainers = [.. containerNames.Where(x => x.EndsWith(attachmentsContainerSuffix)).OrderBy(x => x)];
            Console.WriteLine($"Loaded {attachmentContainers.Count()} containers.");
            Console.WriteLine($"First 10 containers: {string.Join(", ", attachmentContainers.Take(10))}...");

            var sqlClient = GetSqlClient(config);

            foreach (var containerName in attachmentContainers)
            {
                var tenantCode = containerName.Split("-")[1];
                if (processTenant != tenantCode)
                {
                    continue;
                }
                Console.WriteLine($"Starting {tenantCode}...");

                var blobs = await blobStorageClient.ListBlobsInContainerAsync(containerName);
                var dbAttachments = await sqlClient.ReadAttachements(tenantCode);
                var position = 0;
                foreach (var blob in blobs)
                {
                    position++;

                    var contentHash = Convert.ToBase64String(blob.Properties.ContentHash);
                    var contentLength = blob.Properties.ContentLength;
                    var internalStorageId = blob.Name;

                    var attToUpdate = dbAttachments.FirstOrDefault(x => x.AzureStorageBlobName == blob.Name);

                    if (attToUpdate != null)
                    {
                        var updatedRecords = await sqlClient.UpdateAttachmentSizeByIdAsync(attToUpdate.Id, contentHash, contentLength);
                        if (updatedRecords < 1)
                        {
                            Console.WriteLine($"ERROR in Tenant:{tenantCode,-15} RECORDS:{updatedRecords} LEN:{contentLength,-10} HASH:{contentHash} NAME:{internalStorageId}");
                        }
                        Console.WriteLine($"{containerName} {position,10}/{blobs.Count(),-10}");
                    }
                    else
                    {
                        Console.WriteLine($"Attachment {blob.Name} in tenant {tenantCode} was not found in DB.");
                    }
                }
            }
        }

        private static async Task PrintNotExistingDBAttachments(IConfigurationRoot config, string processTenant = "")
        {
            var blobStorageClient = GetBlobStorageClient(config);

            IEnumerable<string> containerNames = await blobStorageClient.ListContainersAsync();
            List<string> attachmentContainers = [.. containerNames.Where(x => x.EndsWith(attachmentsContainerSuffix)).OrderBy(x => x)];
            Console.WriteLine($"Loaded {attachmentContainers.Count()} containers.");

            var sqlClient = GetSqlClient(config);

            foreach (var containerName in attachmentContainers)
            {
                var tenantCode = containerName.Split("-")[1];
                if (processTenant != string.Empty && processTenant != tenantCode)
                {
                    continue;
                }

                var blobs = await blobStorageClient.ListBlobsInContainerAsync(containerName);
                var dbAttachments = await sqlClient.ReadAttachements(tenantCode);
                var position = 0;
                var nonExisting = 0;
                foreach (var blob in blobs)
                {
                    position++;

                    var dbRecordFound = dbAttachments.Any(x => x.AzureStorageBlobName == blob.Name);

                    if (!dbRecordFound)
                    {
                        nonExisting++;
                    }
                }
                Console.WriteLine($"Tenant {tenantCode} has {nonExisting}/{position} attachments in Blob Storage with no record in DB.");
            }
        }


        private static async Task RunBlobContainerUpdate(IConfigurationRoot config, string tenantCode)
        {
            var blobStorageClient = GetBlobStorageClient(config);

            var sqlClient = GetSqlClient(config);
            var attachments = await sqlClient.ReadAttachements(tenantCode);

            Console.WriteLine("Starting properties update...");

            IEnumerable<string> containerNames = await blobStorageClient.ListContainersAsync();
            foreach (var containerName in containerNames.Where(c => c.EndsWith(attachmentsContainerSuffix)))
            {
                var blobs = await blobStorageClient.ListBlobsInContainerAsync(containerName);
                foreach (var blob in blobs.Skip(300))
                {
                    string targetFilename = LookupFilenameByBlobName(attachments, blob.Name);
                    await blobStorageClient.SetBlobPropertiesAsync(containerName, blob.Name, targetFilename);
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
