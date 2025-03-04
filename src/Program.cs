using Azure;
using AzureBlobStorageForeach.DTOs;
using DocumentFormat.OpenXml.InkML;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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
            var companyId = Guid.Parse("7df332c6-94af-4953-8e63-dda370ba273e"); //rhino
            const string excelFilePath = @"C:\Users\HonzaAusterlitz\Downloads\Rhino-vymazat.xlsx";
            const string workSheetNameDelete = "DELETE";
            //const string worksheetNameUpdate = "UPDATE";

            //await ValidateCustomFieldsDataTemplates();

            //await ValidateCustomFieldsTemplates();

            //await ValidateCustomFormTemplates();

            //await UpdateMaterialPriceFromExcel(companyId, excelFilePath, worksheetNameUpdate);

            await MarkMaterialAsDeletedFromExcel(companyId, excelFilePath, workSheetNameDelete);

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

        private static async Task ValidateCustomFieldsDataTemplates()
        {
            var sqlClient = GetSqlClient();

            //QueryStrings.CustomerCustomDataTemplate
            //QueryStrings.OrderCustomDataTemplate
            //QueryStrings.ServiceObjectCustomDataTemplate
            var entities = await sqlClient.ReadCustomDataTemplates(QueryStrings.ServiceObjectCustomDataTemplate);
            string validationName = nameof(QueryStrings.ServiceObjectCustomDataTemplate);

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(2);
            var client = new MWorkRestClient(httpClient);

            string filePath = $"C:\\temp\\{validationName}.log";
            File.Delete(filePath);
            string templatesDir = $"C:\\temp\\{validationName}\\";
            Directory.CreateDirectory(templatesDir);
            using (var writer = new StreamWriter(filePath))
            {
                writer.AutoFlush = true; // Ensure immediate writing to file
                Console.SetOut(writer); // Redirect Console output
                Console.WriteLine($"Number of records is: {entities.Count()}.");

                Console.WriteLine($"-------------------- Validation: {validationName}) --------------------");

                var companyName = entities.FirstOrDefault()?.CompanyName ?? string.Empty;
                foreach (var entity in entities.OrderBy(x => x.CompanyName))
                {
                    if (entity.CompanyName != companyName)
                    {
                        companyName = entity.CompanyName;
                        Console.WriteLine($"-------------------- Company: {companyName} ({entity.TenantCode})) --------------------");
                    }
                        
                    var template = entity.Template.Trim();
                    var customData = entity.CustomData.Trim();

                    if (!template.IsNullOrEmpty() && !customData.IsNullOrEmpty())
                    {
                        string response = await client.MakeMultipartRequestAsync(
                            "http://localhost:5000/main/api/schemas/customFields/data/validate?compatibilityMode=true",
                            HttpMethod.Post,
                            customData, template);
                        ProcessTemplateValidationResponse(template: entity.Id, company: entity.TenantCode, response);
                        File.WriteAllText($"{entity.TenantCode}_{entity.Id}.yml", entity.Template);
                    }
                    else
                    {
                        var timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                        Console.WriteLine($"{timestamp} EMPTY  : {entity.Id}");
                    }
                }
                Console.WriteLine("Done.");
            }
        }


        private static async Task ValidateCustomFieldsTemplates()
        {
            var sqlClient = GetSqlClient();
            var entities = await sqlClient.ReadCompanyTemplates();

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/yaml"));
            var client = new MWorkRestClient(httpClient);

            string filePath = "C:\\temp\\CustomFieldsTemplatesValidation.log";
            File.Delete(filePath);
            string templatesDir = "C:\\temp\\custom_data_templates\\";
            using (var writer = new StreamWriter(filePath))
            {
                writer.AutoFlush = true; // Ensure immediate writing to file
                Console.SetOut(writer); // Redirect Console output
                Console.WriteLine($"Number of records is: {entities.Count()}.");

                foreach (var entity in entities)
                {
                    Console.WriteLine($"-------------------- Company: {entity.Name} ({entity.TenantCode}) --------------------");

                    var template = entity.CustomerCustomDataTemplate.Trim();
                    if (!template.IsNullOrEmpty())
                    {
                        string response = await client.MakeRequestAsync(
                            "http://localhost:5000/main/api/schemas/customFields/template/validate?compatibilityMode=true",
                            HttpMethod.Post,
                            template);
                        ProcessTemplateValidationResponse(template: "CustomerCustomDataTemplate", company: entity.Id.ToString(), response);
                        File.WriteAllText(templatesDir + $"{entity.TenantCode}_CustomerCustomDataTemplate.yml", entity.CustomerCustomDataTemplate);
                    }
                    else
                    {
                        Console.WriteLine($"EMPTY  : template: CustomerCustomDataTemplate");
                    }

                    template = entity.OrderCustomDataTemplate.Trim();
                    if (!template.IsNullOrEmpty())
                    {
                        string response = await client.MakeRequestAsync(
                            "http://localhost:5000/main/api/schemas/customFields/template/validate?compatibilityMode=true",
                            HttpMethod.Post,
                            template);
                        ProcessTemplateValidationResponse(template: "OrderCustomDataTemplate", company: entity.Id.ToString(), response);
                        File.WriteAllText(templatesDir + $"{entity.TenantCode}_OrderCustomDataTemplate.yml", entity.OrderCustomDataTemplate);
                    }
                    else
                    {
                        Console.WriteLine($"EMPTY  : template: OrderCustomDataTemplate");
                    }

                    template = entity.ServiceObjectCustomDataTemplate.Trim();
                    if (!template.IsNullOrEmpty())
                    {
                        string response = await client.MakeRequestAsync(
                            "http://localhost:5000/main/api/schemas/customFields/template/validate?compatibilityMode=true",
                            HttpMethod.Post,
                            template);
                        ProcessTemplateValidationResponse(template: "ServiceObjectCustomDataTemplate", company: entity.Id.ToString(), response);
                        File.WriteAllText(templatesDir + $"{entity.TenantCode}_ServiceObjectCustomDataTemplate.yml", entity.ServiceObjectCustomDataTemplate);
                    }
                    else
                    {
                        Console.WriteLine($"EMPTY  : template: ServiceObjectCustomDataTemplate");
                    }

                    Console.WriteLine();
                }
            }
        }

        private static async Task ValidateCustomFormTemplates()
        {
            var sqlClient = GetSqlClient();
            var entities = await sqlClient.ReadCustomFormTemplates();
            var uniqueTemplates = new Dictionary<string, CustomFormTemplate>();

            foreach (var entity in entities)
            {
                var template = entity.Template;

                // Compute hash of the template
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(template));
                var hashString = Convert.ToHexString(hashBytes); // Convert to a readable string

                // Add to dictionary
                if (!uniqueTemplates.ContainsKey(hashString))
                {
                    uniqueTemplates[hashString] = entity;
                }
            }

            string filePath = "C:\\temp\\output.log";

            // Open a stream to the file
            using (var writer = new StreamWriter(filePath))
            {
                writer.AutoFlush = true; // Ensure immediate writing to file
                Console.SetOut(writer); // Redirect Console output
                Console.WriteLine($"Number of unique Templates is: {uniqueTemplates.Count}.");

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/yaml"));
                var client = new MWorkRestClient(httpClient);

                foreach (var template in uniqueTemplates)
                {
                    if (template.Value.Template.IsNullOrEmpty())
                    {
                        Console.WriteLine($"EMPTY:   {template.Value.Id}");
                        continue;
                    }

                    string response = await client.MakeRequestAsync(
                        "http://localhost:5000/main/api/schemas/customForm/template/validate?compatibilityMode=true",
                        HttpMethod.Post,
                        template.Value.Template);
                    ProcessTemplateValidationResponse(template.Value.Id.ToString(), template.Value.CompanyId.ToString(), response);
                }
                Console.WriteLine("Finished.");
            }
        }

        private static void ProcessTemplateValidationResponse(string template, string company, string response)
        {
            var timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            TemplateValidationResult? validationResult;
            try
            {
                validationResult = JsonSerializer.Deserialize<TemplateValidationResult>(response);
                if (validationResult == null)
                {
                    Console.WriteLine($"{timestamp} INVALID: {template} Company: {company} Response can not be parsed.");
                    return;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"{timestamp} INVALID: {template} Company: {company} Failed to deserialize JSON: {response} Exception: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{timestamp} INVALID: {template} Company: {company} An unexpected error occurred: {ex.Message}");
                return;
            }

            if (validationResult.isValid == false)
            {
                Console.WriteLine($"{timestamp} INVALID: {template} Company: {company} errors: {validationResult.ToString()}");
                return;
            }

            if (validationResult.isValid == true)
            {
                Console.WriteLine($"{timestamp} OK     : {template}, Company: {company}");
            }
        }

        private static async Task MarkMaterialAsDeletedFromExcel(Guid companyId, string excelFilePath, string workSheetNameDelete)
        {
            var excelClient = new ExcelClient();
            var sqlClient = GetSqlClient();
            var materials = excelClient.LoadMaterial(excelFilePath, workSheetNameDelete);

            var cursor = 0;
            foreach (var material in materials)
            {
                cursor++;
                var updatedCount = await sqlClient.MarkMaterialAsDeletedByExternalIdAsync(material.ExternalId, companyId);
                if (!updatedCount)
                {
                    Console.WriteLine($"ERROR ID:{material.ExternalId} name:{material.Name}");
                }

                if (cursor % 100 == 0)
                {
                    Console.WriteLine($"{cursor:D4} / {materials.Count:D4}");
                }
            }
            Console.WriteLine($"{cursor:D4} / {materials.Count:D4}");
        }

        private static async Task UpdateMaterialPriceFromExcel(Guid companyId, string excelFilePath, string workSheetName)
        {
            var excelClient = new ExcelClient();
            var sqlClient = GetSqlClient();
            var materials = excelClient.LoadMaterial(excelFilePath, workSheetName);

            var cursor = 0;
            foreach (var material in materials)
            {
                cursor++;
                if (material.Id != null)
                {
                    var updatedCount = await sqlClient.UpdateMaterialPriceAsync(material.Id ?? Guid.NewGuid(), material.Price, companyId);
                    if (!updatedCount)
                    {
                        Console.WriteLine($"ERROR ID:{material.ExternalId} name:{material.Name}");
                    }
                }

                if (cursor % 100 == 0)
                {
                    Console.WriteLine($"{cursor:D4} / {materials.Count:D4}");
                }
            }
            Console.WriteLine($"{cursor:D4} / {materials.Count:D4}");
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
