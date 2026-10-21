using System.Text.Json;
using Kulani.KDI.Validation;

var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root is not null && !File.Exists(Path.Combine(root.FullName, "Kulani.KDI.slnx"))) root = root.Parent;
if (root is null) throw new InvalidOperationException("KDI root not found");
var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
var failures = ArchitecturePolicy.Check(root.FullName, configuration);
var output = new
{
    suite = "author-structure-check",
    status = failures.Count == 0 ? "PASS" : "FAIL",
    checks = new[] { "project-references", "semantic-layer-references", "infrastructure-composition-only", "public-module-contracts", "module-cycle-check" },
    failures,
    limits = new[] { "Not xUnit or independent review", "No business modules implemented yet; rules must be extended with each module", "Runtime reflection/dynamic loading require review" }
};
var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
var outputDirectory = Path.Combine(root.FullName, "docs/stage3/d2a/evidence");
Directory.CreateDirectory(outputDirectory);
File.WriteAllText(Path.Combine(outputDirectory, "architecture-result.json"), json + Environment.NewLine);
Console.WriteLine(json);
return failures.Count == 0 ? 0 : 1;
