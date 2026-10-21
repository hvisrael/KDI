using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kulani.KDI.Infrastructure.Persistence;

// Every EF runtime connection is checked before any consumer can use it.
public sealed class RuntimePrivilegeInterceptor : DbConnectionInterceptor
{
    internal const string CheckSql = """
        SELECT CASE WHEN CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) = N'DESKTOP-F25O3MF'
          AND DB_NAME() IN (N'KulaniKDI_Dev',N'KulaniKDI_Test')
          AND IS_SRVROLEMEMBER('sysadmin') = 0 AND IS_SRVROLEMEMBER('dbcreator') = 0
          AND IS_MEMBER('db_owner') = 0 AND IS_MEMBER('kdi_application') = 1
          AND HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONTROL') = 0
          AND HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE') = 0
          AND HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE SCHEMA') = 0
          AND HAS_PERMS_BY_NAME(N'identity', 'SCHEMA', 'ALTER') = 0
          AND HAS_PERMS_BY_NAME(N'organizations', 'SCHEMA', 'ALTER') = 0
          AND HAS_PERMS_BY_NAME(N'portfolio', 'SCHEMA', 'ALTER') = 0
          THEN 1 ELSE 0 END;
        """;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = CheckSql;
            if (!Equals(command.ExecuteScalar(), 1)) throw Rejected();
        }
        catch { connection.Close(); throw Rejected(); }
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = CheckSql;
            if (!Equals(await command.ExecuteScalarAsync(cancellationToken), 1)) throw Rejected();
        }
        catch { await connection.CloseAsync(); throw Rejected(); }
    }

    private static InvalidOperationException Rejected() => new("KDI runtime database identity or privileges rejected.");
}
