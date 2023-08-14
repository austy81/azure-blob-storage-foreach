using AzureBlobStorageForeach.DTOs;
using Microsoft.Data.SqlClient;

namespace AzureBlobStorageForeach
{
    public class SqlClient
    {
        private readonly string connectionString;

        public SqlClient(string connectionString) {
            this.connectionString = connectionString;
        }
        public async Task<IEnumerable<Attachment>> ReadAttachements()
        {
            string queryString = """
SELECT
    [InternalStorageId] as AzureStorageBlobName
    ,a.[Name] as FileName
	,c.TenantCode as TenantCode
    ,[IsDeleted]
FROM [dbo].[Attachments] a
join [dbo].Companies c on a.CompanyId = c.Id
""";

            var attachments = new List<Attachment>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(
                    queryString, connection);
                connection.Open();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        attachments.Add(new Attachment(bool.Parse(reader[3].ToString()))
                        {
                            AzureStorageBlobName = reader[0]?.ToString() ?? string.Empty,
                            FileName = reader[1]?.ToString() ?? string.Empty,
                            TenantCode = reader[2]?.ToString() ?? string.Empty,
                        });
                    }
                }
            }
            return attachments;
        }

        public async Task<IEnumerable<CustomDataTenant>> ReadCustomDataTenant(string entityName)
        {
            string queryString = QueryStrings.customDataQueryString.Replace("{entity_name}", entityName);
            var orders = new List<CustomDataTenant>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(
                    queryString, connection);
                connection.Open();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        orders.Add(
                            new CustomDataTenant(
                                reader[0]?.ToString() ?? string.Empty,
                                reader[1]?.ToString() ?? string.Empty));
                    }
                }
            }
            return orders;
        }

    }
}
