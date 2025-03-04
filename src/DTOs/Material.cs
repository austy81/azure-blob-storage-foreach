namespace AzureBlobStorageForeach.DTOs
{
    public class Material
    {
        public Guid? Id { get; set; }

        public string ExternalId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string Code {  get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

}
