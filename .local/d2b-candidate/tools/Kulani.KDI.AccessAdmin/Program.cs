using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Kulani.KDI.Infrastructure.Access;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Microsoft.EntityFrameworkCore;

if (!OperatingSystem.IsWindows()) { Console.Error.WriteLine("Windows operator identity required."); return 2; }
if (args.Length < 3 || args[0] is not ("plan" or "apply"))
{ Console.Error.WriteLine("plan <Dev|Test database> <request.json> <plan.json> | apply <plan.json> <approved-sha256>"); return 2; }
try
{
    var json = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
    Plan plan;
    if (args[0] == "plan")
    {
        if (args.Length != 4) throw new ArgumentException("Four plan arguments required.");
        var request = JsonSerializer.Deserialize<Request>(File.ReadAllText(args[2]), json) ?? throw new ArgumentException("Request required.");
        AccessAdministration.Validate(request);
        plan = new(args[1], request, DateTime.UtcNow, [], WindowsIdentity.GetCurrent().User!.Value);
    }
    else
    {
        if (args.Length != 3) throw new ArgumentException("Three apply arguments required.");
        var bytes = File.ReadAllBytes(args[1]);
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), args[2], StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Plan approval hash mismatch.");
        plan = JsonSerializer.Deserialize<Plan>(bytes, json) ?? throw new ArgumentException("Plan required.");
        AccessAdministration.Validate(plan.Request);
        if (plan.CreatedAtUtc < DateTime.UtcNow.AddMinutes(-30) || plan.CreatedAtUtc > DateTime.UtcNow || plan.OperatorSid != WindowsIdentity.GetCurrent().User!.Value) throw new InvalidOperationException("Plan expired or operator changed.");
    }
    // The session owns the operator credential material and destroys it when the connection is closed.
    await using var session = OperatorConnection.Create(plan.Database);
    var connection = session.Connection;
    await connection.OpenAsync();
    var iOptions = new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")).Options;
    var oOptions = new DbContextOptionsBuilder<OrganizationsDbContext>().UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "organizations")).Options;
    await using var i = new IdentityDbContext(iOptions); await using var o = new OrganizationsDbContext(oOptions);
    await using var tx = await connection.BeginTransactionAsync();
    await i.Database.UseTransactionAsync(tx); await o.Database.UseTransactionAsync(tx);
    await using (var command = connection.CreateCommand())
    {
        command.Transaction = (Microsoft.Data.SqlClient.SqlTransaction)tx;
        command.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'KDI:IdentityAdmin',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=5000,@DbPrincipal='public'; SELECT @r;";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) is not (0 or 1)) throw new InvalidOperationException("Administration lock unavailable.");
    }
    var versions = await AccessAdministration.Versions(i, o);
    if (args[0] == "plan")
    {
        // Plan is a non-mutating, exact state receipt. File contains identifiers and versions only.
        plan = plan with { Versions = versions };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(plan, json)); File.WriteAllBytes(args[3], bytes);
        Console.WriteLine("Plan SHA256=" + Convert.ToHexString(SHA256.HashData(bytes)));
        await tx.RollbackAsync(); return 0;
    }
    if (!versions.SequenceEqual(plan.Versions)) throw new InvalidOperationException("Administrative state changed; generate and review a new plan.");
    await AccessAdministration.ApplyWithinTransactionAsync(i, o, plan.Request, plan.OperatorSid);
    await tx.CommitAsync(); Console.WriteLine("Applied audited operation."); return 0;

}
catch (Exception e) when (e is not OutOfMemoryException)
{ Console.Error.WriteLine("Administrative operation failed; transaction rolled back. Category=" + e.GetType().Name); return 1; }

