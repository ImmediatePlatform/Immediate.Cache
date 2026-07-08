using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Immediate.Cache.Generators;

[Generator]
public sealed partial class ImmediateCacheGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var assemblyDefaults = GetAssemblyDefaults(context);

		var caches = ProcessCaches(context);

		RenderServiceCollectionExtensions(context, assemblyDefaults, caches);
	}

	private static IncrementalValueProvider<AssemblyDefaults> GetAssemblyDefaults(IncrementalGeneratorInitializationContext context)
	{
		var assemblyName = context.CompilationProvider
			.Select((cp, _) => cp.GetAssemblyIdentifier())
			.WithTrackingName("AssemblyName");

		var @namespace = context
			.AnalyzerConfigOptionsProvider
			.Select(
				(c, _) => c.GlobalOptions
					.TryGetValue("build_property.rootnamespace", out var ns)
						? ns : ""
			)
			.WithTrackingName("RootNamespace");

		var assemblyDefaults = assemblyName
			.Combine(@namespace)
			.Select((x, _) => new AssemblyDefaults
			{
				AssemblyName = x.Left,
				RootNamespace = x.Right,
			})
			.WithTrackingName("AssemblyDefaults");

		return assemblyDefaults;
	}

	private static IncrementalValuesProvider<CacheDefinition> ProcessCaches(IncrementalGeneratorInitializationContext context)
	{
		var caches = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"Immediate.Cache.Shared.CacheForAttribute`1",
				(node, _) => node is ClassDeclarationSyntax { IsStatic: false },
				TransformCacheDefinition
			)
			.WhereNotNull()
			.WithTrackingName("CacheDefinitions");

		RenderCaches(context, caches);

		return caches;
	}
}

file static class Extensions
{
	public static string GetAssemblyIdentifier(this Compilation compilation)
	{
		if (compilation.Assembly.GetAttributes()
				.FirstOrDefault(a => a.AttributeClass.IsImmediateAssemblyIdentifierAttribute)
				is { ConstructorArguments: [{ Value: string { Length: >= 1 } identifier }] }
			&& identifier[0] != '@'
			&& SyntaxFacts.IsValidIdentifier(identifier))
		{
			return identifier;
		}

		return (compilation.AssemblyName ?? string.Empty)
			.Replace(".", string.Empty, StringComparison.Ordinal)
			.Replace(" ", string.Empty, StringComparison.Ordinal)
			.Trim();
	}
}
