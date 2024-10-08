﻿using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.Unicode;

namespace AzureBlobStorageForeach
{
    public class BlobStorageClient
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageClient(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<IEnumerable<string>> ListContainersAsync()
        {
            List<string> containerNames = new List<string>();

            await foreach (var container in _blobServiceClient.GetBlobContainersAsync())
            {
                containerNames.Add(container.Name);
            }

            return containerNames;
        }

        public async Task<IEnumerable<BlobItem>> ListBlobsInContainerAsync(string containerName)
        {
            var blobNames = new List<BlobItem>();

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                blobNames.Add(blobItem);
            }

            return blobNames;
        }

        public async Task SetBlobPropertiesAsync(string containerName, string blobName, string targetFilename)
        {
            var isAttachment = containerName.EndsWith("-attachments");
            if (!isAttachment)
            {
                return;
            }

            if (string.IsNullOrEmpty(targetFilename)) {
                Console.WriteLine($"[{containerName}/{blobName}] ATTACHMENTNOTFOUND Attachment was not found in Attachments table.");
                return;
            }

            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);


            try
            {
                // Get the existing properties
                var properties = await blobClient.GetPropertiesAsync();
                var contentDisposition = Utils.GenerateContentDispositionHeader(targetFilename, isAttachment);

                if (properties.Value.ContentDisposition != contentDisposition)
                {
                    var headers = new BlobHttpHeaders
                    {
                        // Populate headers with 
                        // the pre-existing and updated properties
                        ContentType = properties.Value.ContentType,
                        ContentLanguage = properties.Value.ContentLanguage,
                        CacheControl = properties.Value.CacheControl,
                        ContentDisposition = contentDisposition, //properties.ContentDisposition,
                        ContentEncoding = properties.Value.ContentEncoding,
                        ContentHash = properties.Value.ContentHash
                    };
                    var response = await blobClient.SetHttpHeadersAsync(headers);
                    Console.WriteLine($"[{containerName}/{blobName}] UPDATEDCONTENTDISPOSITION {contentDisposition}");
                }
                else
                {
                    Console.WriteLine($"[{containerName}/{blobName}] OKCONTENTDISPOSITION");
                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}
