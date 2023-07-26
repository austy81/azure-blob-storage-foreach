namespace AzureBlobStorageForeach
{
    public class AttachmentDTO
    {
        public string AzureStorageBlobName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string TenantCode { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }

        public bool IsRenamed { get => AzureStorageBlobName.EndsWith(FileName); }

        public bool FilenameHasSuffix { get => FileName.Contains('.'); }

        public bool AzureStorageBlobNameHasSuffix { get => AzureStorageBlobName.Contains('.'); }

        public string AzureStorageBlobNameSuffix { get => AzureStorageBlobNameHasSuffix ? AzureStorageBlobName.Split('.').Last() : string.Empty; }

        public string TargetFilename { get =>
                FilenameHasSuffix ?
                    FileName :
                    AzureStorageBlobNameHasSuffix ?
                        $"{FileName}.{AzureStorageBlobNameSuffix}" :
                        FileName;
        }
    }
}
