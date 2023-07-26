using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobStorageForeach
{
    public class SqlClient
    {
        private readonly string connectionString;

        public SqlClient(string connectionString) {
            this.connectionString = connectionString;
        }
        public IEnumerable<AttachmentDTO> ReadAttachements()
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

            var attachments = new List<AttachmentDTO>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(
                    queryString, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        attachments.Add(new AttachmentDTO()
                        {
                            AzureStorageBlobName = reader[0]?.ToString() ?? string.Empty,
                            FileName = reader[1]?.ToString() ?? string.Empty,
                            TenantCode = reader[2]?.ToString() ?? string.Empty,
                            IsDeleted = bool.Parse(reader[3].ToString()),
                        });
                    }
                }
            }
            return attachments;
        }
    }
}
