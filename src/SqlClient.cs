using AzureBlobStorageForeach.DTOs;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace AzureBlobStorageForeach
{
    public class SqlClient
    {
        private readonly SqlConnection _connection;

        public SqlClient(string connectionString) {
            _connection = new SqlConnection(connectionString);
        }
        public async Task<IEnumerable<Attachment>> ReadAttachements(string tenantCode)
        {
            string queryString = $@"
SELECT
    a.Id as AttachmentId,
    [InternalStorageId] as AzureStorageBlobName,
    a.[Name] as FileName,
	c.TenantCode as TenantCode,
    [IsDeleted]
FROM [dbo].[Attachments] a
join [dbo].Companies c on a.CompanyId = c.Id
WHERE c.TenantCode = '{tenantCode}'
";

            var attachments = new List<Attachment>();

            await OpenConnectionIfNeeded();
            var command = new SqlCommand(queryString, _connection);

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    attachments.Add(new Attachment(bool.Parse(reader[4].ToString()!))
                    {
                        Id = Guid.Parse(reader[0].ToString()!),
                        AzureStorageBlobName = reader[1]?.ToString() ?? string.Empty,
                        FileName = reader[2]?.ToString() ?? string.Empty,
                        TenantCode = reader[3]?.ToString() ?? string.Empty,
                    });
                }
            }
            return attachments;
        }

        public async Task<IEnumerable<CustomDataTenant>> ReadCustomDataTenant(string entityName)
        {
            string queryString = QueryStrings.customDataQueryString.Replace("{entity_name}", entityName);
            var orders = new List<CustomDataTenant>();

            await OpenConnectionIfNeeded();
            var command = new SqlCommand(queryString, _connection);

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
            return orders;
        }

        internal async Task<int> UpdateAttachmentSizeByInternalStorageIdAsync(string tenantCode, string internalStorageId, string contentHash, long? contentLength)
        {
            var cmd = @"
            update Attachments 
            set 
                MD5Hash = @contentHash,
                SizeBytes = @contentLength
            from
                Attachments a
                join Companies c on a.CompanyId = c.Id
            where 
                a.InternalStorageId COLLATE Latin1_General_CI_AI = @internalStorageId
                and c.TenantCode = @tenantCode
            ";

            await OpenConnectionIfNeeded();

            SqlCommand command = new SqlCommand(cmd, _connection);
            command.Parameters.AddWithValue("@contentHash", contentHash);
            command.Parameters.AddWithValue("@contentLength", contentLength.HasValue ? contentLength.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@internalStorageId", internalStorageId);
            command.Parameters.AddWithValue("@tenantCode", tenantCode);

            return await command.ExecuteNonQueryAsync();
        }

        internal async Task<int> UpdateAttachmentSizeByIdAsync(Guid attachmentId, string contentHash, long? contentLength)
        {
            var cmd = @"
            update Attachments 
            set 
                MD5Hash = @contentHash,
                SizeBytes = @contentLength
            from
                Attachments a
            where 
                a.Id = @attachmentId
            ";

            await OpenConnectionIfNeeded();

            SqlCommand command = new SqlCommand(cmd, _connection);
            command.Parameters.AddWithValue("@contentHash", contentHash);
            command.Parameters.AddWithValue("@contentLength", contentLength.HasValue ? contentLength.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@attachmentId", attachmentId);

            return await command.ExecuteNonQueryAsync();
        }

        private async Task OpenConnectionIfNeeded()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();  // Ensure the connection is open
            }
        }

        // Dispose method to close and dispose the connection when done
        public void Dispose()
        {
            if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
            {
                _connection.Close();
            }
            _connection?.Dispose();
        }
    }
}
