using System.Reflection;
using Microsoft.CodeAnalysis;
using Scriban;

namespace Immediate.Cache.Generators;

public sealed partial class ImmediateCacheGenerator
{
	private static readonly Template ServiceCollectionExtensionsTemplate = GetTemplate("ServiceCollectionExtensions");
	private static readonly Template ApplicationCacheTemplate = GetTemplate("ApplicationCache");

	private static void RenderServiceCollectionExtensions(
		IncrementalGeneratorInitializationContext context,
		IncrementalValueProvider<AssemblyDefaults> assemblyDefaults,
		IncrementalValuesProvider<CacheDefinition> caches
	)
	{
		context.RegisterSourceOutput(
			assemblyDefaults.Combine(caches.Collect()),
			(context, x) =>
			{
				var source = ServiceCollectionExtensionsTemplate
					.Render(new
					{
						x.Left.AssemblyName,
						x.Left.RootNamespace,

						Caches = x.Right,

						Version = ThisAssembly.InformationalVersion,
					});

				context.CancellationToken.ThrowIfCancellationRequested();
				context.AddSource("IC.ServiceCollectionExtensions.g.cs", source);
			}
		);
	}

	private static void RenderCaches(
		IncrementalGeneratorInitializationContext context,
		IncrementalValuesProvider<CacheDefinition> caches
	)
	{
		context.RegisterSourceOutput(
			caches,
			(context, cache) =>
			{
				var source = ApplicationCacheTemplate
					.Render(new
					{
						cache.Namespace,
						cache.ClassName,
						cache.RequestType,
						cache.ResponseType,

						Version = ThisAssembly.InformationalVersion,
					});

				context.CancellationToken.ThrowIfCancellationRequested();
				context.AddSource($"IC.{cache.Namespace}.{cache.ClassName}.g.cs", source);
			}
		);
	}

	private static Template GetTemplate(string name)
	{
		using var stream = Assembly
			.GetExecutingAssembly()
			.GetManifestResourceStream(
				$"Immediate.Cache.Generators.Templates.{name}.sbntxt"
			);

		using var reader = new StreamReader(stream);
		return Template.Parse(reader.ReadToEnd());
	}
}
