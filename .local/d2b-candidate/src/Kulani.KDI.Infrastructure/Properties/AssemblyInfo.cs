using System.Runtime.CompilerServices;

// The operator credential seams are internal so that only the unit test project can exercise them.
// The operator entry point that operators use stays public and keeps the protected console guard.
[assembly: InternalsVisibleTo("Kulani.KDI.UnitTests")]
// The fixture credential of the SQL test harness is internal too. The harness project is the only
// other consumer that may build it, and the product never reaches it.
[assembly: InternalsVisibleTo("Kulani.KDI.SqlIntegrationTests")]
