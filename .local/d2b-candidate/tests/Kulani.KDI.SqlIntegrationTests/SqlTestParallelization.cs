using Xunit.Sdk;
using Xunit.v3;

// The D2-B SQL harness works on a single shared database. Two SQL cases running at the same time would let
// the organization of the access fixture, a migration probe of the baseline class or a cleanup statement of
// one case interfere with the assertions of another one. Serializing the whole assembly is the mechanism
// that removes that interference inside the process of the test run: only one SQL case touches the database
// at any moment.
//
// The serialization is PROCESS LOCAL. It does not exclude any other invocation of the test runner, it does
// not stop another session of the operator from touching the database, and it does not lock the base. The
// exclusion of any other writer is an operational decision of the plan, not a property of the harness.
//
// xUnit does not guarantee the order of the classes, and the harness does not rely on one. The access
// fixture organization can only still be present when an administration case starts if the cleanup of the
// access case failed, and that situation is detected and reported by the administration fixture with the
// exact identifiers, see SqlAdministratorInvariant and D2bAdministrationSqlTests.
//
// xUnit v3 4.0 replaced CollectionBehavior.DisableTestParallelization with this attribute; the old property
// is obsolete and no longer callable.
[assembly: Parallelization(Mode = ParallelMode.None)]
