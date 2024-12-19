namespace AzureBlobStorageForeach.DTOs
{
    public record CompanyTemplate(Guid Id, string Name, string TenantCode, string CustomerCustomDataTemplate, string OrderCustomDataTemplate, string ServiceObjectCustomDataTemplate);
}