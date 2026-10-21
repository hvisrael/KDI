using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kulani.KDI.Validation;

public static class ArchitecturePolicy
{
    private static readonly Dictionary<string, string[]> Layers = new()
    {
        ["Domain"] = [], ["Contracts"] = [],
        ["Application"] = ["Domain", "Contracts"],
        ["Infrastructure"] = ["Application", "Domain", "Contracts"],
        ["Api"] = ["Application", "Contracts", "Infrastructure"]
    };

    public static IReadOnlyList<string> Check(string root, string configuration)
    {
        var failures = new HashSet<string>();
        foreach (var (layer, allowed) in Layers)
        {
            var project = XDocument.Load(Path.Combine(root, "src", $"Kulani.KDI.{layer}", $"Kulani.KDI.{layer}.csproj"));
            var refs = project.Descendants("ProjectReference")
                .Select(x => Path.GetFileNameWithoutExtension(x.Attribute("Include")!.Value.Replace('/', Path.DirectorySeparatorChar)))
                .Select(x => x!.Replace("Kulani.KDI.", "")).Order().ToArray();
            if (!refs.SequenceEqual(allowed.Order())) failures.Add($"{layer}: unexpected project reference graph");
            if (layer is "Domain" or "Contracts" && project.Descendants("PackageReference").Any())
                failures.Add($"{layer}: external package reference forbidden");
        }

        var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).ToList();
        referencePaths.AddRange(Layers.Keys.Select(layer => Path.Combine(root, "src", $"Kulani.KDI.{layer}", "bin", configuration, "net10.0", $"Kulani.KDI.{layer}.dll")));
        // Include resolved provider assemblies when compiling the product source semantically.
        // These are the exact built application dependencies, not an unrelated package cache.
        referencePaths.AddRange(Directory.GetFiles(Path.Combine(root, "src", "Kulani.KDI.Api", "bin", configuration, "net10.0"), "*.dll"));
        var graph = new Dictionary<string, HashSet<string>>();
        foreach (var (layer, allowed) in Layers)
        {
            var layerRoot = Path.Combine(root, "src", $"Kulani.KDI.{layer}");
            var sources = Directory.GetFiles(layerRoot, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Split(Path.DirectorySeparatorChar).Any(s => s is "obj" or "bin")).ToList();
            sources.Add(Path.Combine(layerRoot, "obj", configuration, "net10.0", $"Kulani.KDI.{layer}.GlobalUsings.g.cs"));
            var trees = sources.Select(p => CSharpSyntaxTree.ParseText(File.ReadAllText(p), path: p)).ToArray();
            var references = referencePaths.Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(p => Path.GetFileName(p) != $"Kulani.KDI.{layer}.dll")
                .Select(p => MetadataReference.CreateFromFile(p));
            var compilation = CSharpCompilation.Create($"Kulani.KDI.{layer}", trees, references,
                new CSharpCompilationOptions(layer == "Api" ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary));
            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            foreach (var error in errors) failures.Add(error.ToString());
            if (errors.Length != 0) continue; // Do not classify unresolved semantic checks as PASS.
            foreach (var tree in trees)
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var node in tree.GetRoot().DescendantNodes().OfType<NameSyntax>())
                {
                    var symbol = model.GetSymbolInfo(node).Symbol;
                    if (symbol is IAliasSymbol alias) symbol = alias.Target;
                    // A qualified public facade includes namespace prefixes that are not themselves dependencies.
                    if (symbol is INamespaceSymbol && node.Parent is NameSyntax) continue;
                    var targetNamespace = (symbol as INamespaceSymbol)?.ToDisplayString() ?? symbol?.ContainingNamespace?.ToDisplayString();
                    if (targetNamespace is null) continue;
                    var parts = targetNamespace.Split('.');
                    if (parts.Length >= 3 && parts[0] == "Kulani" && parts[1] == "KDI" && parts[2] != layer && !allowed.Contains(parts[2]))
                        failures.Add($"{tree.FilePath}: forbidden source dependency {targetNamespace}");
                    if (layer == "Api" && IsNamespaceWithin(targetNamespace, "Kulani.KDI.Infrastructure") && !IsCompositionRoot(root, tree.FilePath))
                        failures.Add($"{tree.FilePath}: Infrastructure is restricted to Program.cs composition");

                    var owner = Module(node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString());
                    var target = Module(targetNamespace);
                    if (owner is null || target is null || owner == target) continue;
                    if (!graph.TryGetValue(owner, out var edges)) graph[owner] = edges = [];
                    edges.Add(target);
                    if (!ModuleAccessAllowed(owner, targetNamespace)) failures.Add($"{tree.FilePath}: forbidden cross-module access {targetNamespace}");
                    if (symbol is not null and not INamespaceSymbol && !IsPublicSurface(symbol))
                        failures.Add($"{tree.FilePath}: cross-module access must use public contracts/facades");
                }
            }
        }
        if (HasCycle(graph)) failures.Add("Business module dependency cycle detected");
        return failures.Order().ToArray();
    }

    public static bool ModuleAccessAllowed(string owner, string targetNamespace) =>
        Module(targetNamespace) is not { } target || owner == target
        || ((IsNamespaceWithin(targetNamespace, $"Kulani.KDI.Contracts.Modules.{target}")
            || IsNamespaceWithin(targetNamespace, $"Kulani.KDI.Application.Modules.{target}.Public"))
            && !targetNamespace.Split('.').Any(part => part is "Persistence" or "Internal"));

    public static bool IsCompositionRoot(string root, string sourcePath)
    {
        static string Normalize(string path) => Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
        var expected = Normalize(Path.Combine(root, "src", "Kulani.KDI.Api", "Program.cs"));
        return string.Equals(expected, Normalize(sourcePath), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    public static bool IsPublicSurface(ISymbol symbol)
    {
        if (symbol.DeclaredAccessibility is not (Accessibility.Public or Accessibility.NotApplicable)) return false;
        for (var type = symbol as INamedTypeSymbol ?? symbol.ContainingType; type is not null; type = type.ContainingType)
            if (type.DeclaredAccessibility != Accessibility.Public) return false;
        return true;
    }

    private static bool IsNamespaceWithin(string value, string parent) =>
        value == parent || value.StartsWith(parent + ".", StringComparison.Ordinal);

    private static string? Module(string? ns)
    {
        var parts = ns?.Split('.');
        var index = parts is null ? -1 : Array.IndexOf(parts, "Modules");
        return index >= 0 && parts!.Length > index + 1 ? parts[index + 1] : null;
    }

    public static bool HasCycle(Dictionary<string, HashSet<string>> graph)
    {
        var visiting = new HashSet<string>();
        var visited = new HashSet<string>();
        bool Visit(string node)
        {
            if (visiting.Contains(node)) return true;
            if (!visited.Add(node)) return false;
            visiting.Add(node);
            if (graph.TryGetValue(node, out var edges) && edges.Any(Visit)) return true;
            visiting.Remove(node);
            return false;
        }
        return graph.Keys.Any(Visit);
    }
}
