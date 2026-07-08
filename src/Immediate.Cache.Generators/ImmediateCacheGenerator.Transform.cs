using Microsoft.CodeAnalysis;

namespace Immediate.Cache.Generators;

public sealed partial class ImmediateCacheGenerator
{
	private static CacheDefinition? TransformCacheDefinition(GeneratorAttributeSyntaxContext context, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		if (context.TargetSymbol is not INamedTypeSymbol { ContainingType: null } targetSymbol)
			return null;

		if (context.Attributes is not [{ AttributeClass.TypeArguments: [INamedTypeSymbol { IsStatic: false } handlerSymbol] }])
			return null;

		var @namespace = targetSymbol.ContainingNamespace.ToDisplayString().NullIf("<global namespace>");
		var name = targetSymbol.Name;

		if (!handlerSymbol.GetValidHandleMethod(out var requestType, out var responseType))
			return null;

		return new CacheDefinition()
		{
			Namespace = @namespace,
			ClassName = name,
			RequestType = requestType.ToDisplayString(DisplayNameFormatters.FullyQualifiedWithNullableFormat),
			ResponseType = responseType.ToDisplayString(DisplayNameFormatters.FullyQualifiedWithNullableFormat),
		};
	}
}
