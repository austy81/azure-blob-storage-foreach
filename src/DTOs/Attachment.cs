namespace AzureBlobStorageForeach.DTOs
{
    public record Attachment(bool IsDeleted)
    {
        public string AzureStorageBlobName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string TenantCode { get; set; } = string.Empty;

        public bool IsRenamed { get => AzureStorageBlobName.EndsWith(FileName); }

        public bool FilenameHasSuffix { get => FileName.Contains('.'); }

        public bool AzureStorageBlobNameHasSuffix { get => AzureStorageBlobName.Contains('.'); }

        public string AzureStorageBlobNameSuffix { get => AzureStorageBlobNameHasSuffix ? AzureStorageBlobName.Split('.').Last() : string.Empty; }

        public string TargetFilename
        {
            get =>
                FilenameHasSuffix ?
                    FileName :
                    AzureStorageBlobNameHasSuffix ?
                        $"{FileName}.{AzureStorageBlobNameSuffix}" :
                        FileName;
        }
    }
}
