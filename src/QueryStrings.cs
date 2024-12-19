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

        public static string CompanyTemplates = """
            select 
                Id, Name, TenantCode, CustomerCustomDataTemplate, OrderCustomDataTemplate, ServiceObjectCustomDataTemplate
            from
                Companies
            where
                IsDisabled = 0
            """;

        public static string CustomerCustomDataTemplate = """
            select 
                c.Id,
            	c.CustomData,
            	co.CustomerCustomDataTemplate,
            	co.TenantCode,
            	co.Name as CompanyName
            from
            	Customers c
            	join Companies co on c.CompanyId = co.Id
            where
            	c.CustomData != '' and
            	c.IsDeleted = 0 and 
            	co.IsDisabled = 0
            """;

        public static string OrderCustomDataTemplate = """
            select
                o.Id,
            	o.CustomData,
                co.OrderCustomDataTemplate,
                co.TenantCode, 
                co.Name as CompanyName
            from
            	Orders o
            	join Companies co on o.CompanyId = co.Id
            where
            	o.CustomData != '' and
            	o.IsDeleted = 0 and 
            	co.IsDisabled = 0
            """;

        public static string ServiceObjectCustomDataTemplate = """
            select
                s.Id,
            	s.CustomData,
                c.CustomDataTemplate,
                co.TenantCode,
                co.Name as CompanyName
            from
            	ServiceObjects s
            	join Companies co on s.CompanyId = co.Id
            	join ServiceObjectCategories c on c.Id = s.CategoryId
            where
            	s.CustomData != '' and
            	s.IsDeleted = 0 and 
            	co.IsDisabled = 0
            """;
    }
}
