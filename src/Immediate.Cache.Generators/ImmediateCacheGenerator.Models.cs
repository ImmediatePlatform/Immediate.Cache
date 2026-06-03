using System.Diagnostics.CodeAnalysis;

namespace Immediate.Cache.Generators;

public sealed partial class ImmediateCacheGenerator
{
	[ExcludeFromCodeCoverage]
	private sealed record AssemblyDefaults
	{
		public required string AssemblyName { get; init; }
		public required string RootNamespace { get; init; }
	}

	[ExcludeFromCodeCoverage]
	private sealed record CacheDefinition
	{
		public required string? Namespace { get; init; }
		public required string ClassName { get; init; }
		public required string RequestType { get; init; }
		public required string ResponseType { get; init; }
	}
}
