using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Immediate.Cache.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CacheForUsageAnalyzer : DiagnosticAnalyzer
{
	public static readonly DiagnosticDescriptor CacheMustNotBeNested =
		new(
			id: DiagnosticIds.IC0001CacheMustNotBeNested,
			title: "Cache nesting is not allowed",
			messageFormat: "Cache '{0}' must not be nested in another type",
			category: "ImmediateCache",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Cache classes must not be nested in another type.",
			customTags: [WellKnownDiagnosticTags.NotConfigurable]
		);

	public static readonly DiagnosticDescriptor TargetMustBeHandler =
		new(
			id: DiagnosticIds.IC0002TargetMustBeHandler,
			title: "Cache Target must be a `[Handler]`",
			messageFormat: "Cache Target class '{0}' is not marked as `[Handler]`",
			category: "ImmediateCache",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "IC Caches explicitly wrap IH Handlers only.",
			customTags: [WellKnownDiagnosticTags.NotConfigurable]
		);

	public static readonly DiagnosticDescriptor TargetHandlerMustReturnValue =
		new(
			id: DiagnosticIds.IC0003TargetHandlerMustReturnValue,
			title: "Cache Target Handler must have return value",
			messageFormat: "Cache Target class '{0}' must return a value",
			category: "ImmediateCache",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Caching a non-value does not .",
			customTags: [WellKnownDiagnosticTags.NotConfigurable]
		);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		ImmutableArray.Create(
		[
			CacheMustNotBeNested,
			TargetMustBeHandler,
			TargetHandlerMustReturnValue,
		]);

	public override void Initialize(AnalysisContext context)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
	}

	private static void AnalyzeSymbol(SymbolAnalysisContext context)
	{
		var token = context.CancellationToken;
		token.ThrowIfCancellationRequested();

		var cacheSymbol = (INamedTypeSymbol)context.Symbol;

		if (cacheSymbol.GetCacheTargetHandler() is not { } targetTypeSymbol)
			return;

		if (cacheSymbol.ContainingType is not null)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					CacheMustNotBeNested,
					cacheSymbol.Locations[0],
					cacheSymbol.Name)
			);
		}

		if (targetTypeSymbol is not INamedTypeSymbol { IsHandler: true } handlerSymbol)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					TargetMustBeHandler,
					cacheSymbol.Locations[0],
					targetTypeSymbol.Name
				)
			);

			return;
		}

		if (
			handlerSymbol.GetHandleMethod() is not
			{
				ReturnType: INamedTypeSymbol
				{
					Arity: 1,
					Name: "ValueTask",
					ContainingNamespace.IsSystemThreadingTasks: true,
					TypeArguments: [ITypeSymbol],
				},
			}
		)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					TargetHandlerMustReturnValue,
					cacheSymbol.Locations[0],
					targetTypeSymbol.Name
				)
			);
		}
	}
}

file static class Extensions
{
	public static ITypeSymbol? GetCacheTargetHandler(this INamedTypeSymbol typeSymbol)
	{
		foreach (var attribute in typeSymbol.GetAttributes())
		{
			if (attribute.AttributeClass is { IsCacheForAttribute: true, TypeArguments: [var targetTypeSymbol] })
				return targetTypeSymbol;
		}

		return null;
	}
}
