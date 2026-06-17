using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SqlScripter.Models;

namespace SqlScripter.Services;

/// <summary>
/// Thin data-access layer over a MSSQL server. Only responsible for opening a
/// connection and enumerating user objects; the heavy lifting of producing
/// scripts lives in <see cref="SqlScriptGenerator"/>.
/// </summary>
public sealed class DatabaseService
{
    /// <summary>Opens the connection briefly to validate the connection string.</summary>
    public async Task TestConnectionAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
    }

    /// <summary>
    /// Returns every user table, view and stored procedure in the database the
    /// connection string points at (system / MS-shipped objects are excluded).
    /// </summary>
    public async Task<IReadOnlyList<DbObjectInfo>> GetObjectsAsync(string connectionString, CancellationToken ct = default)
    {
        const string sql = """
            SELECT s.name AS SchemaName, o.name AS ObjectName, o.type AS ObjType
            FROM sys.objects o
            INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
            WHERE o.type IN ('U', 'V', 'P') AND o.is_ms_shipped = 0
            ORDER BY o.type, s.name, o.name;
            """;

        var result = new List<DbObjectInfo>();

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            var name = reader.GetString(1);
            var code = reader.GetString(2).Trim();

            SqlObjectType? type = code switch
            {
                "U" => SqlObjectType.Table,
                "V" => SqlObjectType.View,
                "P" => SqlObjectType.StoredProcedure,
                _ => null
            };

            if (type is null)
                continue;

            result.Add(new DbObjectInfo { Schema = schema, Name = name, Type = type.Value });
        }

        return result;
    }
}
