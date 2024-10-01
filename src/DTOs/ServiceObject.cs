namespace AzureBlobStorageForeach.DTOs
{
    public class ServiceObject
    {
        public Guid Id { get; set; }

        public string ExternalId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IdentificationNumber { get; set; } = string.Empty;
        public string NextRevisionPeriodInMonths { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

}
