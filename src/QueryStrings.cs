namespace AzureBlobStorageForeach
{
    public static class QueryStrings
    {
        public static string customDataQueryString = """
SELECT --TOP 40000
    o.[CustomData]
    ,c.TenantCode as TenantCode
FROM [dbo].[{entity_name}] o
join [dbo].Companies c on o.CompanyId = c.Id
where o.CustomData != ''
ORDER BY o.Modified DESC
""";

        public static string customFormTemplatesQueryString = """
            select 
                Id, CompanyId, Template 
            from
                CustomFormTemplates
            where
                IsDeleted = 0
            """;
    }
}
