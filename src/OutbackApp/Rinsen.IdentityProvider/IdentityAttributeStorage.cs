using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace Rinsen.IdentityProvider;

public class IdentityAttributeStorage : IIdentityAttributeStorage
{
    private readonly IdentityOptions _identityOptions;

    private const string GetSql = @"SELECT 
                                            Attribute,
                                            Value,
                                            Id,
                                            IdentityId,
                                            Created,
                                            Updated,
                                            Deleted
                                        FROM 
                                            IdentityAttributes 
                                        WHERE 
                                            IdentityId=@IdentityId AND Deleted is null";

    private const string _insertSql = @"INSERT INTO IdentityAttributes 
                                        (Attribute,
                                        Value,
                                        IdentityId,
                                        Created,
                                        Updated,
                                        Deleted) 
                                    VALUES 
                                        (@Attribute,
                                        @Value,
                                        @IdentityId,
                                        @Created,
                                        @Updated,
                                        @Deleted) 
                                    SELECT CAST(SCOPE_IDENTITY() as int)";

    private const string DeleteSql = @"UPDATE 
                                            IdentityAttributes 
                                        SET 
                                            Deleted = @Deleted,
                                            Updated = @Updated
                                        WHERE 
                                            Id = @Id";

    private const string CountAdministratorsSql = @"SELECT COUNT(*)
                                                    FROM IdentityAttributes ia
                                                    INNER JOIN Identities i ON i.IdentityId = ia.IdentityId
                                                    WHERE ia.Attribute = 'Administrator'
                                                      AND ia.Deleted IS NULL
                                                      AND i.Deleted IS NULL";

    public IdentityAttributeStorage(IdentityOptions identityOptions)
    {
        _identityOptions = identityOptions;
    }

    public async Task<IEnumerable<IdentityAttribute>> GetIdentityAttributesAsync(Guid identityId)
    {
        var result = new List<IdentityAttribute>();

        using var connection = new SqlConnection(_identityOptions.ConnectionString);
        using var command = new SqlCommand(GetSql, connection);

        command.Parameters.Add(new SqlParameter("@IdentityId", identityId));

        await connection.OpenAsync();
        using (var reader = await command.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new IdentityAttribute
                    {
                        Attribute = (string)reader["Attribute"],
                        Value = (string)reader["Value"],
                        Id = (int)reader["Id"],
                        IdentityId = (Guid)reader["IdentityId"],
                        Created = (DateTimeOffset)reader["Created"],
                        Updated = (DateTimeOffset)reader["Updated"],
                        Deleted = reader.GetValueOrDefault<DateTimeOffset?>("Deleted")
                    });
                }
            }
        }

        return result;
    }

    public async Task CreateAsync(Identity identity, string attribute, string value = "")
    {
        var now = DateTimeOffset.Now;

        using var connection = new SqlConnection(_identityOptions.ConnectionString);
        await connection.OpenAsync();

        // The unique constraint on (IdentityId, Attribute) also covers
        // soft-deleted rows, so re-granting an attribute must restore the
        // existing row instead of inserting a new one.
        int? existingId = null;
        using (var check = new SqlCommand(
            "SELECT Id FROM IdentityAttributes WHERE IdentityId = @IdentityId AND Attribute = @Attribute", connection))
        {
            check.Parameters.Add(new SqlParameter("@IdentityId", identity.IdentityId));
            check.Parameters.Add(new SqlParameter("@Attribute", attribute));

            using var reader = await check.ExecuteReaderAsync();
            if (reader.HasRows && await reader.ReadAsync())
            {
                existingId = (int)reader["Id"];
            }
        }

        if (existingId is not null)
        {
            using var restore = new SqlCommand(
                "UPDATE IdentityAttributes SET Deleted = NULL, Updated = @Updated WHERE Id = @Id", connection);
            restore.Parameters.Add(new SqlParameter("@Updated", now));
            restore.Parameters.Add(new SqlParameter("@Id", existingId.Value));
            await restore.ExecuteNonQueryAsync();
            return;
        }

        using var command = new SqlCommand(_insertSql, connection);

        command.Parameters.Add(new SqlParameter("@Created", now));
        command.Parameters.Add(new SqlParameter("@Updated", now));
        command.Parameters.Add(new SqlParameter("@IdentityId", identity.IdentityId));
        command.Parameters.Add(new SqlParameter("@Attribute", attribute));
        command.Parameters.Add(new SqlParameter("@Value", value));
        command.Parameters.AddWithNullableValue("@Deleted", (DateTimeOffset?)null);

        await command.ExecuteScalarAsync();
    }

    public async Task DeleteAsync(IdentityAttribute identityAttribute)
    {
        var now = DateTimeOffset.Now;

        using var connection = new SqlConnection(_identityOptions.ConnectionString);
        using var command = new SqlCommand(DeleteSql, connection);

        command.Parameters.Add(new SqlParameter("@Deleted", now));
        command.Parameters.Add(new SqlParameter("@Updated", now));
        command.Parameters.Add(new SqlParameter("@Id", identityAttribute.Id));

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> CountAdministratorsAsync()
    {
        using var connection = new SqlConnection(_identityOptions.ConnectionString);
        using var command = new SqlCommand(CountAdministratorsSql, connection);

        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();

        return result == null ? 0 : Convert.ToInt32(result);
    }
}
