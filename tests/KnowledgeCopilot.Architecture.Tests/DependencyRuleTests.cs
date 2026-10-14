using System.Reflection;
using System.Xml.Linq;
using KnowledgeCopilot.Adapters.Fakes;
using KnowledgeCopilot.Domain.Ports;
using NetArchTest.Rules;

namespace KnowledgeCopilot.Architecture.Tests;

/// <summary>The dependency rule (architecture §4): Domain and Application never see vendor SDKs or outer layers.</summary>
public sealed class DependencyRuleTests
{
    private static readonly string[] VendorNamespaces =
    [
        "Azure",
        "Qdrant",
        "OllamaSharp",
        "Npgsql",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.SqlClient",
    ];

    private static readonly string[] OuterLayerNamespaces =
    [
        "KnowledgeCopilot.Adapters",
        "KnowledgeCopilot.Persistence",
        "KnowledgeCopilot.Api",
        "KnowledgeCopilot.Worker",
        "KnowledgeCopilot.ServiceDefaults",
    ];

    private static readonly Assembly DomainAssembly = typeof(IIndexStore).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(KnowledgeCopilot.Application.ServiceCollectionExtensions).Assembly;

    public static TheoryData<string> CoreAssemblies => ["KnowledgeCopilot.Domain", "KnowledgeCopilot.Application"];

    [Theory]
    [MemberData(nameof(CoreAssemblies))]
    public void Types_CoreAssembly_HaveNoVendorDependency(string assemblyName)
    {
        AssertNoDependency(Load(assemblyName), VendorNamespaces);
    }

    [Theory]
    [MemberData(nameof(CoreAssemblies))]
    public void Types_CoreAssembly_HaveNoOuterLayerDependency(string assemblyName)
    {
        AssertNoDependency(Load(assemblyName), OuterLayerNamespaces);
    }

    [Fact]
    public void Types_Domain_HaveNoApplicationDependency()
    {
        AssertNoDependency(DomainAssembly, ["KnowledgeCopilot.Application"]);
    }

    [Theory]
    [MemberData(nameof(CoreAssemblies))]
    public void ReferencedAssemblies_CoreAssembly_IncludeNoVendorAssembly(string assemblyName)
    {
        var vendor = Load(assemblyName).GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(IsVendor)
            .ToList();

        Assert.True(vendor.Count == 0, $"{assemblyName} references vendor assemblies: {string.Join(", ", vendor)}");
    }

    // Catches a vendor package in the project file even before any type from it is used.
    [Theory]
    [MemberData(nameof(CoreAssemblies))]
    public void PackageReferences_CoreProject_IncludeNoVendorPackage(string assemblyName)
    {
        var project = XDocument.Load(Path.Combine(RepositoryRoot(), "src", assemblyName, $"{assemblyName}.csproj"));

        var vendor = project.Descendants("PackageReference")
            .Select(reference => (string?)reference.Attribute("Include") ?? string.Empty)
            .Where(IsVendor)
            .ToList();

        Assert.True(vendor.Count == 0, $"{assemblyName}.csproj references vendor packages: {string.Join(", ", vendor)}");
    }

    // Positive control: proves the rule engine sees dependencies at all, so the tests above can fail.
    [Fact]
    public void Types_FakesAdapter_DependsOnDomain()
    {
        var dependents = Types.InAssembly(typeof(InMemoryIndexStore).Assembly)
            .That()
            .HaveDependencyOnAny("KnowledgeCopilot.Domain")
            .GetTypes()
            .Select(type => type.FullName);

        Assert.Contains(typeof(InMemoryIndexStore).FullName, dependents);
    }

    private static void AssertNoDependency(Assembly assembly, string[] forbidden)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        var failing = result.FailingTypes?.Select(type => $"{type.FullName}: {type.Explanation}") ?? [];
        Assert.True(
            result.IsSuccessful,
            $"{assembly.GetName().Name} must not depend on [{string.Join(", ", forbidden)}]. Violations:{Environment.NewLine}{string.Join(Environment.NewLine, failing)}");
    }

    private static Assembly Load(string assemblyName) => assemblyName switch
    {
        "KnowledgeCopilot.Domain" => DomainAssembly,
        "KnowledgeCopilot.Application" => ApplicationAssembly,
        _ => throw new ArgumentOutOfRangeException(nameof(assemblyName), assemblyName, null),
    };

    private static bool IsVendor(string name) =>
        VendorNamespaces.Any(prefix =>
            name.Equals(prefix, StringComparison.Ordinal) || name.StartsWith(prefix + ".", StringComparison.Ordinal));

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KnowledgeCopilot.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not find KnowledgeCopilot.slnx above the test output directory.");
    }
}
