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

        public async Task<IEnumerable<CustomFormTemplate>> ReadCustomFormTemplates()
        {
            string queryString = QueryStrings.customFormTemplatesQueryString;
            var entities = new List<CustomFormTemplate>();

            await OpenConnectionIfNeeded();
            var command = new SqlCommand(queryString, _connection);

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    entities.Add(
                        new CustomFormTemplate(
                            Id: Guid.Parse(reader[0]?.ToString() ?? string.Empty),
                            CompanyId: Guid.Parse(reader[1]?.ToString() ?? string.Empty),
                            Template: reader[2]?.ToString() ?? string.Empty)
                        );
                }
            }
            return entities;
        }

        public async Task<IEnumerable<CompanyTemplate>> ReadCompanyTemplates()
        {
            var entities = new List<CompanyTemplate>();

            await OpenConnectionIfNeeded();
            var command = new SqlCommand(QueryStrings.CompanyTemplates, _connection);

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    entities.Add(
                        new CompanyTemplate(
                            Id: Guid.Parse(reader[0]?.ToString() ?? string.Empty),
                            Name: reader[1]?.ToString() ?? string.Empty,
                            TenantCode: reader[2]?.ToString() ?? string.Empty,
                            CustomerCustomDataTemplate: reader[3]?.ToString() ?? string.Empty,
                            OrderCustomDataTemplate: reader[4]?.ToString() ?? string.Empty,
                            ServiceObjectCustomDataTemplate: reader[5]?.ToString() ?? string.Empty)
                        );
                }
            }
            return entities;
        }

        public async Task<IEnumerable<CustomDataTemplate>> ReadCustomDataTemplates(string query)
        {
            var entities = new List<CustomDataTemplate>();

            await OpenConnectionIfNeeded();
            var command = new SqlCommand(query, _connection);

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    entities.Add(
                        new CustomDataTemplate(
                            Id: reader[0]?.ToString() ?? string.Empty,
                            CustomData: reader[1]?.ToString() ?? string.Empty,
                            Template: reader[2]?.ToString() ?? string.Empty,
                            TenantCode: reader[3]?.ToString() ?? string.Empty,
                            CompanyName: reader[4]?.ToString() ?? string.Empty
                            )
                        );
                }
            }
            return entities;
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

        internal async Task<int> UpdateServiceObjectAsync(ServiceObject serviceObject)
        {
            if (serviceObject == null)
                return 0;

            var cmd = @"
            update ServiceObjects 
            set 
                Name = @contentName,
                IdentificationNumber = @contentIdentificationNumber,
                NextRevisionPeriodInMonths = @contentNRPM,
                Description = @contentDescription
            from
                ServiceObjects s
            where 
                s.Id = @serviceObjectId and CompanyId = 'AF7AEB3E-12E4-4DF5-AA5E-9F0E9696EA6D'
            ";

            await OpenConnectionIfNeeded();

            SqlCommand command = new SqlCommand(cmd, _connection);
            command.Parameters.AddWithValue("@contentName", serviceObject.Name);
            command.Parameters.AddWithValue("@contentIdentificationNumber", serviceObject.IdentificationNumber);
            command.Parameters.AddWithValue("@contentNRPM", serviceObject.NextRevisionPeriodInMonths);
            command.Parameters.AddWithValue("@contentDescription", serviceObject.Description);
            command.Parameters.AddWithValue("@serviceObjectId", serviceObject.Id);

            return await command.ExecuteNonQueryAsync();
        }

        internal async Task<bool> IsMovedServiceObject(Guid serviceObjectId, Guid companyId)
        {
            var cmd = @"
                select 
                    count(*)
                from 
                    ServiceObjects s
                where 
                    s.Id = @serviceObjectId 
                    and s.CompanyId = @companyId
                    and s.IsMoved = 1 or s.OriginServiceObjectId is Not Null)
                ";

            await OpenConnectionIfNeeded();

            using (SqlCommand command = new SqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@serviceObjectId", serviceObjectId);
                command.Parameters.AddWithValue("@companyId", companyId);

                var result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result) > 0;
            }
        }

        internal async Task<bool> IsOriginalServiceObject(Guid serviceObjectId, Guid companyId)
        {
            var cmd = @"
                select 
                    count(*)
                from 
                    ServiceObjects s
                where 
                    s.Id = @serviceObjectId 
                    and s.CompanyId = @companyId
                    and s.OriginServiceObjectId is Not Null
                ";

            await OpenConnectionIfNeeded();

            using (SqlCommand command = new SqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@serviceObjectId", serviceObjectId);
                command.Parameters.AddWithValue("@companyId", companyId);

                var result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result) > 0;
            }
        }

        internal async Task<bool> MarkServiceObjectDeleted(Guid serviceObjectId, Guid companyId)
        {
            var cmd = @"
                update
                    ServiceObjects
                set
                    IsDeleted = 1
                from 
                    ServiceObjects s
                where 
                    s.Id = @serviceObjectId 
                    and s.CompanyId = @companyId
                ";

            await OpenConnectionIfNeeded();

            using (SqlCommand command = new SqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@serviceObjectId", serviceObjectId);
                command.Parameters.AddWithValue("@companyId", companyId);

                var result = await command.ExecuteNonQueryAsync();

                return result == 1;
            }
        }

        internal async Task<bool> UpdateServiceObjectNameAsync(string serviceObjectName, string serviceObjectExternalId, Guid companyId)
        {
            var cmd = @"
                update
                    ServiceObjects
                set
                    Name = @serviceObjectName
                from 
                    ServiceObjects s
                where 
                    s.ExternalId = @serviceObjectExternalId 
                    and s.CompanyId = @companyId
                ";

            await OpenConnectionIfNeeded();

            using (SqlCommand command = new SqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@serviceObjectExternalId", serviceObjectExternalId);
                command.Parameters.AddWithValue("@companyId", companyId);
                command.Parameters.AddWithValue("@serviceObjectName", serviceObjectName);

                var result = await command.ExecuteNonQueryAsync();

                return result == 1;
            }
        }

        internal async Task<bool> MarkMaterialAsDeletedAsync(Guid materialId, Guid companyId)
        {
            var cmd = @"
                update
                    Material
                set
                    IsDeleted = 1,
                    Modified = GETDATE()
                from 
                    Material m
                where 
                    m.Id = @materialId 
                    and m.CompanyId = @companyId
                ";

            await OpenConnectionIfNeeded();

            using (SqlCommand command = new SqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@materialId", materialId);
                command.Parameters.AddWithValue("@companyId", companyId);

                var result = await command.ExecuteNonQueryAsync();

                return result == 1;
            }
        }

        internal async Task<bool> MarkMaterialAsDeletedByExternalIdAsync(string externalId, Guid companyId)
        {
            var cmd = @"
                update
                    Material
                set
                    IsDeleted = 1,
                    Modified = GETDATE()
                from 
                    Material m
                where 
                    m.ExternalId = @externalId 
                    and m.CompanyId = @companyId
                ";

            await OpenConnectionIfNeeded();

            using (SqlCommand command = new SqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@externalId", externalId);
                command.Parameters.AddWithValue("@companyId", companyId);

                var result = await command.ExecuteNonQueryAsync();

                return result == 1;
            }
        }

        internal async Task<bool> ServiceObjectHasOpenOrderAsync(Guid serviceObjectId, Guid companyId)
        {
            var cmd = @"
                select count(*) OrderId from ServiceObjects so
                join OrderServiceObjects oso on so.id = oso.ServiceObjectId
                join Orders o on oso.OrderId = o.Id
                join Companies c on c.Id = so.CompanyId
                where 
	                so.Id = @serviceObjectId
	                and c.Id = @companyId 
	                and so.IsDeleted = 0
	                and o.State = 'Open'
                ";

            await OpenConnectionIfNeeded();

            using (SqlCommand command = new SqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@serviceObjectId", serviceObjectId);
                command.Parameters.AddWithValue("@companyId", companyId);

                var result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result) > 0;
            }
        }

        internal async Task<bool> UpdateMaterialPriceAsync(Guid materialId, decimal materialPrice, Guid companyId)
        {
            var cmd = @"
                update
                    Material
                set
                    Price = @materialPrice,
                    Modified = GETDATE()
                from 
                    Material m
                where 
                    m.Id = @materialId 
                    and m.CompanyId = @companyId
                ";

            await OpenConnectionIfNeeded();

            using (SqlCommand command = new SqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@materialId", materialId);
                command.Parameters.AddWithValue("@materialPrice", materialPrice);
                command.Parameters.AddWithValue("@companyId", companyId);

                var result = await command.ExecuteNonQueryAsync();

                return result == 1;
            }
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

        internal async Task<bool> UpdateContactPeopleAsync(string phonePrefix, string phoneNumber, string externalId, Guid companyId)
        {
            var cmd = @"
                update
                    ContactPeople
                set
                    PhonePrefix = @phonePrefix,
                    PhoneNumber = @phoneNumber,
                    Modified = GETDATE()
                from 
                    ContactPeople c
                where 
                    c.ExternalId = @externalId
                    and c.CompanyId = @companyId
                ";

            await OpenConnectionIfNeeded();

            using (SqlCommand command = new SqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@phonePrefix", phonePrefix);
                command.Parameters.AddWithValue("@phoneNumber", phoneNumber);
                command.Parameters.AddWithValue("@externalId", externalId);
                command.Parameters.AddWithValue("@companyId", companyId);

                var result = await command.ExecuteNonQueryAsync();

                return result == 1;
            }
        }
    }
}
