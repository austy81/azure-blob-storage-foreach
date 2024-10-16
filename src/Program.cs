using AzureBlobStorageForeach.DTOs;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AzureBlobStorageForeach
{
    internal class Program
    {
        private const string attachmentsContainerSuffix = "-attachments";
        private static readonly IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                //.AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .Build();

        static async Task Main(string[] args)
        {
            var companyId = Guid.Parse("53A8CFD7-E50A-4B16-845D-9215445D9E9F"); //horia
            const string excelFilePath = "C:\\Users\\HonzaAusterlitz\\Downloads\\Update material - horia.xlsx";
            const string workSheetNameDelete = "DELETE";
            //const string worksheetNameUpdate = "change name";

            await DeleteMaterialFromExcel(companyId, excelFilePath, workSheetNameDelete);

            //await UpdateServiceObjectNameFromExcel(companyId, excelFilePath, worksheetNameUpdate);

            //await ServiceObjectsInfo(companyId, excelFilePath, workSheetNameDelete);

            //await MarkServiceObjectAsDeletedFromExcel(companyId, excelFilePath, workSheetNameDelete);

            //await PrintNotExistingDBAttachments();

            //await UpdateAttachmentsSize();

            //await RunBlobContainerUpdate();

            //var sqlClient = GetSqlClient();
            //AuditCustomData(await sqlClient.ReadCustomDataTenant("Orders"), "Orders");
            //AuditCustomData(await sqlClient.ReadCustomDataTenant("ServiceObjects"), "ServiceObjects");
            //AuditCustomData(await sqlClient.ReadCustomDataTenant("Customers"), "Customers");
        }

        private static async Task DeleteMaterialFromExcel(Guid companyId, string excelFilePath, string workSheetNameDelete)
        {
            var excelClient = new ExcelClient();
            var sqlClient = GetSqlClient();
            var materials = excelClient.LoadMaterial(excelFilePath, workSheetNameDelete);

            var cursor = 0;
            foreach (var material in materials)
            {
                cursor++;
                var updatedCount = await sqlClient.MarkMaterialAsDeletedAsync(material.Id, companyId);
                if (!updatedCount)
                {
                    Console.WriteLine($"ERROR ID:{material.ExternalId} name:{material.Name}");
                }

                if (cursor % 1 == 0)
                {
                    Console.WriteLine($"{cursor:D4} / {materials.Count:D4}");
                }
            }
        }

        private static async Task UpdateServiceObjectNameFromExcel(Guid companyId, string filePath, string worksheetName)
        {
            var excelClient = new ExcelClient();
            var sqlClient = GetSqlClient();
            var serviceObjects = excelClient.LoadServiceObjects(filePath, worksheetName);

            var cursor = 0;
            foreach (var serviceObject in serviceObjects)
            {
                cursor++;
                var updatedCount = await sqlClient.UpdateServiceObjectNameAsync(serviceObject.Name, serviceObject.ExternalId, companyId);
                if (!updatedCount)
                {
                    Console.WriteLine($"ERROR ID:{serviceObject.ExternalId} name:{serviceObject.Name}");
                }

                if (cursor % 200 == 0)
                {
                    Console.WriteLine($"{cursor:D4} / {serviceObjects.Count:D4}");
                }
            }
        }


        private static async Task MarkServiceObjectAsDeletedFromExcel(Guid companyId, string filePath, string worksheetName)
        {
            var excelClient = new ExcelClient();
            var sqlClient = GetSqlClient();
            var serviceObjects = excelClient.LoadServiceObjects(filePath, worksheetName);

            var cursor = 0;
            foreach (var serviceObject in serviceObjects)
            {
                cursor++;
                var updatedCount = await sqlClient.MarkServiceObjectDeleted(serviceObject.Id, companyId);
                if (!updatedCount)
                {
                    Console.WriteLine($"ERROR ID:{serviceObject.Id} name:{serviceObject.Name}");
                }

                if (cursor % 200 == 0)
                {
                    Console.WriteLine($"{cursor:D4} / {serviceObjects.Count:D4}");
                }
            }
        }

        private static async Task ServiceObjectsInfo(Guid companyId, string excelFilePath, string workSheetName)
        {
            var excelClient = new ExcelClient();
            var sqlClient = GetSqlClient();

            var serviceObjects = excelClient.LoadServiceObjects(excelFilePath, workSheetName);
            var cursor = 0;
            foreach (var serviceObject in serviceObjects)
            {
                cursor++;
                //var isMoved = await sqlClient.IsMovedServiceObject(serviceObject.Id, companyId);
                //var hasOpenOrder = await sqlClient.ServiceObjectHasOpenOrderAsync(serviceObject.Id, companyId);
                var isOriginalServiceObject = await sqlClient.IsOriginalServiceObject(serviceObject.Id, companyId);
                if (isOriginalServiceObject)
                {
                    Console.WriteLine($"isOriginalServiceObject Id:{serviceObject.Id}");
                }
                if (cursor % 200 == 0)
                {
                    Console.WriteLine($"{cursor:D4} / {serviceObjects.Count:D4}");
                }
            }
            Console.WriteLine($"Checked {cursor} records.");
        }

        private static void AuditCustomData(IEnumerable<CustomDataTenant>? entityList, string auditedObject)
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

        private static async Task UpdateAttachmentsSize(string processTenant = "schlieger")
        {
            var blobStorageClient = GetBlobStorageClient();

            IEnumerable<string> containerNames = await blobStorageClient.ListContainersAsync();
            List<string> attachmentContainers = [.. containerNames.Where(x => x.EndsWith(attachmentsContainerSuffix)).OrderBy(x => x)];
            Console.WriteLine($"Loaded {attachmentContainers.Count()} containers.");
            Console.WriteLine($"First 10 containers: {string.Join(", ", attachmentContainers.Take(10))}...");

            var sqlClient = GetSqlClient();

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

        private static async Task PrintNotExistingDBAttachments(string processTenant = "")
        {
            var blobStorageClient = GetBlobStorageClient();

            IEnumerable<string> containerNames = await blobStorageClient.ListContainersAsync();
            List<string> attachmentContainers = [.. containerNames.Where(x => x.EndsWith(attachmentsContainerSuffix)).OrderBy(x => x)];
            Console.WriteLine($"Loaded {attachmentContainers.Count()} containers.");

            var sqlClient = GetSqlClient();

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


        private static async Task RunBlobContainerUpdate(string tenantCode)
        {
            var blobStorageClient = GetBlobStorageClient();

            var sqlClient = GetSqlClient();
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

        private static BlobStorageClient GetBlobStorageClient()
        {
            var connectionString = config.GetConnectionString("BlobStorageProd");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("BlobStorage connection string is empty.");
            }

            var blobStorageClient = new BlobStorageClient(connectionString);
            return blobStorageClient;
        }

        private static SqlClient GetSqlClient()
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
