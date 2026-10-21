using System.Linq.Expressions;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Kulani.KDI.SqlIntegrationTests;

// Result of one cleanup pass. Every statement and every verification is isolated: a failing one is recorded
// and the remaining ones still run, so one failure never hides the residue of the others, and the report is
// always complete enough to be written to the artifact.
internal sealed class CleanupReport
{
    public List<CleanupStatementResult> Statements { get; } = [];
    public List<ResidualCheck> Checks { get; } = [];
    public List<ResidualRow> Residuals { get; } = [];
    public bool HasResidue => Residuals.Count > 0;
    public bool HasFailures => Statements.Any(x => x.Error is not null) || Checks.Any(x => x.Error is not null);
    public string DescribeResiduals() => Residuals.Count == 0
        ? "no residue"
        : string.Join("; ", Residuals.Select(x => x.Table + ": " + x.Count + " row(s) [" + string.Join(", ", x.Ids) + "]"));
    public string DescribeChecks() => string.Join("; ", Checks.Select(x => x.Table + "=" + x.Count));
    public string DescribeFailures()
    {
        var statements = Statements.Where(x => x.Error is not null).Select(x => x.Statement + ": " + x.Error + (x.Note is null ? "" : " (" + x.Note + ")"));
        var checks = Checks.Where(x => x.Error is not null).Select(x => "verify " + x.Table + ": " + x.Error);
        var all = statements.Concat(checks).ToList();
        return all.Count == 0 ? "no failure" : string.Join("; ", all);
    }
}

// Result of the verification of one table after the cleanup. The count of every table is reported, also
// when it is zero, so that a residue of zero is explicit and not inferred from the absence of a failure. A
// verification that could not be executed is reported with its error instead of being skipped in silence.
internal sealed record ResidualCheck
{
    public string Table { get; init; } = "";
    public string Owner { get; init; } = "";
    public int Count { get; init; }
    public string? Error { get; init; }
}

internal sealed record CleanupStatementResult
{
    public string Statement { get; init; } = "";
    public string Owner { get; init; } = "";
    public int AffectedRows { get; init; }
    public string? Error { get; init; }
    // Reason of the failure when the harness knows it and it is safe to write, for example the refusal of the
    // scope of the audit rows. It is empty for the errors of the database, whose text can carry row values.
    public string? Note { get; init; }
}

internal sealed record ResidualRow
{
    public string Table { get; init; } = "";
    public int Count { get; init; }
    public IReadOnlyList<string> Ids { get; init; } = [];
}

internal static class SqlCleanup
{
    // Runs one cleanup statement in isolation. The failure is captured, never propagated, and the caller
    // decides what to report. The error is reduced to its type and, for SQL, its error number, so the
    // artifact never carries values of the rows it could not remove.
    public static async Task<int> IsolatedAsync(CleanupReport report, string statement, Func<CancellationToken, Task<int>> action, CancellationToken token)
    {
        try
        {
            var affected = await action(token);
            report.Statements.Add(new CleanupStatementResult { Statement = statement, AffectedRows = affected });
            return affected;
        }
        catch (Exception error)
        {
            report.Statements.Add(new CleanupStatementResult { Statement = statement, Error = Describe(error) });
            return -1;
        }
    }

    // Verifies that one table has no row of the case left. The verification is isolated like the statements:
    // a failure is recorded and the remaining verifications still run, so that the report of the cleanup is
    // never lost because of a single failed query.
    public static async Task RecordResidualAsync<T>(CleanupReport report, string table, IQueryable<T> source, Expression<Func<T, Guid>> id, CancellationToken token)
    {
        try
        {
            var ids = await source.Select(id).ToArrayAsync(token);
            report.Checks.Add(new ResidualCheck { Table = table, Count = ids.Length });
            if (ids.Length > 0) report.Residuals.Add(new ResidualRow { Table = table, Count = ids.Length, Ids = ids.Select(x => x.ToString()).ToList() });
        }
        catch (Exception error)
        {
            report.Checks.Add(new ResidualCheck { Table = table, Count = -1, Error = Describe(error) });
        }
    }

    public static string Describe(Exception error) => error is SqlException sql ? error.GetType().Name + "(" + sql.Number + ")" : error.GetType().Name;
}
