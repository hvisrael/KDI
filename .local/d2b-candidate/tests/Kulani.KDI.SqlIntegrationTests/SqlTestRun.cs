using System.Text.Json;

namespace Kulani.KDI.SqlIntegrationTests;

// Identifiers, cleanup bookkeeping and recovery artifact of one execution of the D2-B SQL harness.
// Every case of the run shares the operator and the correlation of the run, and every case registers the
// identifiers of the rows it creates, so every delete statement can be scoped exactly to the case that
// created the row, and the artifact names the identifiers of the fixtures of the run and the rows that a
// reviewed manual recovery would have to remove. Nothing here is secret: the artifact only carries
// identifiers, counts, table names and error codes.
internal sealed class SqlTestRun
{
    private static readonly Lazy<SqlTestRun> Lazy = new(() => new SqlTestRun());
    private static readonly JsonSerializerOptions ArtifactOptions = new() { WriteIndented = true };
    private readonly object gate = new();
    private readonly Dictionary<Guid, string> users = [];
    private readonly Dictionary<Guid, string> organizations = [];
    private readonly Dictionary<string, string> subjects = [];
    private readonly List<CleanupStatementResult> cleanupStatements = [];
    private readonly List<ResidualCheck> residualChecks = [];
    private readonly List<ResidualRow> residuals = [];
    private readonly List<InvariantSnapshot> invariantSnapshots = [];
    private string? lastState;

    // Known warnings of the stage, written to the artifact so that the operator sees them with the evidence
    // of the run. The historical residue is NOT removed by this harness: the rows written by the previous
    // version of the harness carry the fixed operator "S-1-5-21-SYNTHETIC-TEST" and are outside the scope of
    // any case, because the scope of a case is limited to the identifiers of that case. Removing them would
    // mean a block delete of the audit by a fixed actor, which the security review of the stage forbids
    // ("no borrar todo por el SID sintético compartido", SECURITY-SQL-REVIEW.md).
    // They are information for the plan: a reviewed manual cleanup by exact identifier and by action.
    //
    // The second warning is a finding of the plan, not a defect of the harness: the harness needs INSERT and
    // DELETE on the three schemas, and the role kdi_tests of KulaniKDI_Test has only SELECT today, so a run
    // that is executed with the login kdi_test_runner would fail on the permissions until the plan grants
    // them. With KDI_SQL_TEST_IDENTITY=SQL-KDI-TEST-RUNNER the harness authenticates with that login and its
    // DML runs with exactly the permissions of kdi_tests; without that switch the harness keeps the
    // historical Windows identity of the process. The permissions are a decision of the plan of D2-B and are
    // not changed here.
    private static readonly string[] Known =
    [
        "Historical residue: audit rows written by the previous version of the harness carry the fixed actor 'S-1-5-21-SYNTHETIC-TEST' and are not inside the scope of any case of this run. They need a reviewed manual cleanup by exact identifier, action and time window.",
        "Plan finding, not a defect of the harness: the harness needs INSERT and DELETE on the identity, organizations and portfolio schemas. The role kdi_tests of KulaniKDI_Test has only SELECT on the three schemas, so a run that is executed with the login kdi_test_runner would fail on the permissions until the plan grants them, and could leave orphan sessions and audit rows. The permissions are a decision of the plan of D2-B and are not changed here."
    ];

    private SqlTestRun()
    {
        RunId = Guid.NewGuid();
        // Run scoped operator identity. It replaces the shared constant that used to write audit rows that
        // no cleanup could reach. It fits the 184 characters of identity.AuditEntries.ActorId and the 64
        // characters of the correlation columns. The audit scope refuses to build a scope with an operator
        // that does not have this form, see SqlAuditScope. The operator is not part of any delete predicate.
        OperatorSid = "KDI-D2B-RUN-" + RunId.ToString("N");
        Correlation = OperatorSid;
        ArtifactPath = ResolveArtifactPath(RunId);
        StartedUtc = DateTime.UtcNow;
    }

    public static SqlTestRun Current => Lazy.Value;

    public Guid RunId { get; }
    public string OperatorSid { get; }
    public string Correlation { get; }
    public string ArtifactPath { get; }
    public DateTime StartedUtc { get; }

    public void RegisterUser(Guid id, string owner) { lock (gate) { users[id] = owner; } Write(); }
    public void RegisterOrganization(Guid id, string owner) { lock (gate) { organizations[id] = owner; } Write(); }
    // The subject of a synthetic user is registered before the user is created, so that a process that dies
    // while the user is being created still leaves the value that locates the row in identity.Users.
    public void RegisterSubject(string subject, string owner) { lock (gate) { subjects[subject] = owner; } Write(); }
    public void RegisterUsers(IEnumerable<Guid> ids, string owner)
    {
        lock (gate) { foreach (var id in ids) users[id] = owner; }
        Write();
    }
    public void RegisterOrganizations(IEnumerable<Guid> ids, string owner)
    {
        lock (gate) { foreach (var id in ids) organizations[id] = owner; }
        Write();
    }
    public void RecordCleanup(CleanupReport report, string owner)
    {
        lock (gate)
        {
            foreach (var statement in report.Statements) cleanupStatements.Add(statement with { Owner = owner });
            foreach (var check in report.Checks) residualChecks.Add(check with { Owner = owner });
            residuals.AddRange(report.Residuals);
        }
        Write();
    }
    public void RecordInvariant(InvariantSnapshot snapshot) { lock (gate) { invariantSnapshots.Add(snapshot); } Write(); }
    public string? OwnerOfOrganization(Guid id) { lock (gate) { return organizations.GetValueOrDefault(id); } }
    public IReadOnlyList<(Guid Id, string Owner)> RegisteredOrganizations { get { lock (gate) { return organizations.Select(x => (x.Key, x.Value)).ToList(); } } }

    // The known warnings of the stage are written to the output of the case, so that they are visible to the
    // operator even when the case fails before it writes the rest of its evidence.
    public void WriteKnownWarnings()
    {
        foreach (var warning in Known) SqlTestOutput.Write("KNOWN WARNING (d2b-sql-run " + RunId.ToString("N") + "): " + warning);
    }

    // The artifact lives in .local of the candidate, or in the working directory when the candidate cannot
    // be located. The path is configurable so that an approved run can write it wherever the evidence is
    // collected, and the real execution SHOULD fix KDI_D2B_SQL_RUN_ARTIFACT to the approved evidence folder:
    // the default path falls inside the frozen tree of the candidate, in a directory that the manifest
    // excludes, so it does not change any hash, but the evidence of an approved run is better collected
    // outside of the candidate. The file is replaced by a rename, so a reader does not find a half written
    // file in its place.
    private static string ResolveArtifactPath(Guid runId)
    {
        var configured = Environment.GetEnvironmentVariable("KDI_D2B_SQL_RUN_ARTIFACT");
        if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured.Trim());
        var root = CandidateRoot();
        var directory = root is null ? Path.Combine(WorkingDirectory(), ".local", "d2b-runs") : Path.Combine(root, ".local", "d2b-runs");
        return Path.Combine(directory, "d2b-sql-run-" + runId.ToString("N") + ".json");
    }
    private static string? CandidateRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, ".local")) && Directory.Exists(Path.Combine(directory.FullName, "tests")))
                return directory.FullName;
        return null;
    }
    private static string WorkingDirectory()
    {
        try { return Directory.GetCurrentDirectory(); }
        catch (Exception) { return AppContext.BaseDirectory; }
    }

    // The artifact is rewritten on every registration, so that an interrupted process always leaves the
    // identifiers of the last registered fixture on disk. The cost is one serialization and one atomic file
    // replace per registration; the D2-B harness registers a few dozen identifiers per run, so the cost is
    // negligible next to the round trips to the database. A write whose state did not change is skipped: the
    // comparison uses the document without the update timestamp, because that timestamp changes on every
    // call and would make the comparison always different.
    private void Write()
    {
        lock (gate)
        {
            try
            {
                var document = new RunArtifact
                {
                    RunId = RunId.ToString("N"),
                    OperatorSid = OperatorSid,
                    ArtifactPath = ArtifactPath,
                    StartedUtc = StartedUtc,
                    UpdatedUtc = StartedUtc,
                    // The identifiers of the rows are written in the default format of a Guid, the same one that
                    // SqlAccessStore and AccessAdministration write in the ActorId column, so that the
                    // artifact and the database can be compared without any conversion.
                    Users = users.Select(x => new OwnedIdentifier { Id = x.Key.ToString(), Owner = x.Value }).ToList(),
                    Organizations = organizations.Select(x => new OwnedIdentifier { Id = x.Key.ToString(), Owner = x.Value }).ToList(),
                    Subjects = subjects.Select(x => new OwnedIdentifier { Id = x.Key, Owner = x.Value }).ToList(),
                    CleanupStatements = cleanupStatements.ToList(),
                    ResidualChecks = residualChecks.ToList(),
                    Residuals = residuals.ToList(),
                    InvariantSnapshots = invariantSnapshots.ToList(),
                    KnownWarnings = Known
                };
                var state = JsonSerializer.Serialize(document, ArtifactOptions);
                if (state == lastState) return;
                document.UpdatedUtc = DateTime.UtcNow;
                var content = JsonSerializer.Serialize(document, ArtifactOptions);
                var directory = Path.GetDirectoryName(ArtifactPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var temporary = ArtifactPath + ".tmp";
                File.WriteAllText(temporary, content);
                File.Move(temporary, ArtifactPath, overwrite: true);
                lastState = state;
            }
            catch (Exception error)
            {
                // The artifact is the recovery evidence of the run. If it cannot be written the failure has
                // to be visible, because a run without artifact has no exact list of identifiers to recover.
                SqlTestOutput.Write("BLOCKED: the D2-B run artifact could not be written (" + error.GetType().Name + "): " + ArtifactPath);
            }
        }
    }
}

internal sealed class RunArtifact
{
    public string RunId { get; set; } = "";
    public string OperatorSid { get; set; } = "";
    public string ArtifactPath { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public List<OwnedIdentifier> Users { get; set; } = [];
    public List<OwnedIdentifier> Organizations { get; set; } = [];
    public List<OwnedIdentifier> Subjects { get; set; } = [];
    public List<CleanupStatementResult> CleanupStatements { get; set; } = [];
    public List<ResidualCheck> ResidualChecks { get; set; } = [];
    public List<ResidualRow> Residuals { get; set; } = [];
    public List<InvariantSnapshot> InvariantSnapshots { get; set; } = [];
    public string[] KnownWarnings { get; set; } = [];
}

internal sealed class OwnedIdentifier
{
    public string Id { get; set; } = "";
    public string Owner { get; set; } = "";
}

// Output of the harness: the test output of the running case and the console of the test host.
internal static class SqlTestOutput
{
    public static void Write(string message)
    {
        try { TestContext.Current.TestOutputHelper?.WriteLine(message); }
        catch (Exception)
        {
            // The output helper only exists while a test is running. The artifact and the console keep the detail.
        }
        Console.WriteLine(message);
    }
}
