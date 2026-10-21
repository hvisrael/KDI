using Kulani.KDI.Validation;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Kulani.KDI.ArchitectureTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void References_and_sources_respect_architecture()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Kulani.KDI.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        Assert.Empty(ArchitecturePolicy.Check(root.FullName, configuration));
    }

    [Fact]
    public void Detects_a_two_module_cycle()
    {
        Assert.True(ArchitecturePolicy.HasCycle(new() { ["Organizations"] = ["Portfolio"], ["Portfolio"] = ["Organizations"] }));
        Assert.False(ArchitecturePolicy.HasCycle(new() { ["Portfolio"] = ["Organizations"] }));
    }

    [Theory]
    [InlineData("Kulani.KDI.Infrastructure.Modules.Portfolio.Persistence", false)]
    [InlineData("Kulani.KDI.Domain.Modules.Portfolio", false)]
    [InlineData("Kulani.KDI.Contracts.Modules.Portfolio.V1", true)]
    [InlineData("Kulani.KDI.Application.Modules.Portfolio.Public", true)]
    [InlineData("Kulani.KDI.Application.Modules.Portfolio.Public.V1", true)]
    [InlineData("Kulani.KDI.Application.Modules.Portfolio", false)]
    [InlineData("Kulani.KDI.Application.Modules.Portfolio.Internal", false)]
    [InlineData("Kulani.KDI.Application.Modules.Portfolio.Publicity", false)]
    [InlineData("Kulani.KDI.Application.Modules.Portfolio.Public.Internal", false)]
    [InlineData("Kulani.KDI.Contracts.Modules.Portfolio.Internal", false)]
    [InlineData("Kulani.KDI.Contracts.Modules.Portfolio.Persistence", false)]
    [InlineData("Kulani.KDI.ContractsExtra.Modules.Portfolio.V1", false)]
    [InlineData("Kulani.KDI.Domain.Modules.Organizations", true)]
    public void Enforces_module_ownership(string targetNamespace, bool expected) =>
        Assert.Equal(expected, ArchitecturePolicy.ModuleAccessAllowed("Organizations", targetNamespace));

    [Theory]
    [InlineData("src/Kulani.KDI.Api/Program.cs", true)]
    [InlineData("src\\Kulani.KDI.Api\\Program.cs", true)]
    [InlineData("src/Kulani.KDI.Api/Endpoints/Program.cs", false)]
    [InlineData("src/Kulani.KDI.ApiExtra/Program.cs", false)]
    [InlineData("src/Kulani.KDI.Api/../Kulani.KDI.Application/Program.cs", false)]
    public void Restricts_composition_to_the_exact_normalized_source_path(string relativePath, bool expected)
    {
        var root = Path.Combine(Path.GetTempPath(), "kdi-architecture-policy");
        Assert.Equal(expected, ArchitecturePolicy.IsCompositionRoot(root, Path.Combine(root, relativePath)));
    }

    [Theory]
    [InlineData("PublicFacade", true)]
    [InlineData("InternalFacade", false)]
    [InlineData("HiddenContainer+PublicFacade", false)]
    [InlineData("PublicContainer+PublicFacade", true)]
    public void Requires_public_facades_and_public_containing_types(string metadataName, bool expected)
    {
        var compilation = CSharpCompilation.Create("FacadeVisibility", [CSharpSyntaxTree.ParseText("""
            public class PublicFacade { public void Execute() { } }
            internal class InternalFacade { public void Execute() { } }
            internal class HiddenContainer { public class PublicFacade { public void Execute() { } } }
            public class PublicContainer { public class PublicFacade { public void Execute() { } } }
            """, cancellationToken: TestContext.Current.CancellationToken)], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var type = compilation.GetTypeByMetadataName(metadataName)!;
        Assert.Equal(expected, ArchitecturePolicy.IsPublicSurface(type));
        Assert.Equal(expected, ArchitecturePolicy.IsPublicSurface(type.GetMembers("Execute").Single()));
    }
}
