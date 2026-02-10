using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Immediate.Cache.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OwnedDisposableScopeSuppressor : DiagnosticSuppressor
{
	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions =>
		ImmutableArray.Create([
			new SuppressionDescriptor(
				id: "OwnedDisposableScopeSuppression",
				suppressedDiagnosticId: "CA2000",
				justification: "Suppress disposable not being disposed when used from `Owned<T>.GetScope(out T)`."
			),
		]);
	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		var token = context.CancellationToken;

		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			token.ThrowIfCancellationRequested();

			var syntaxTree = diagnostic.Location.SourceTree;

			if (syntaxTree
					?.GetRoot(token)
					.FindNode(diagnostic.Location.SourceSpan) is not ArgumentSyntax
					{
						Parent.Parent: InvocationExpressionSyntax
						{
							Expression: MemberAccessExpressionSyntax
							{
								Name: IdentifierNameSyntax
								{
									Identifier.Text: "GetScope",
								},

								Expression: { } expression,
							},
						},
					})
			{
				continue;
			}

			var symbol = context.GetSemanticModel(syntaxTree).GetSymbolInfo(expression, token).Symbol switch
			{
				IParameterSymbol ps => ps.Type,
				IPropertySymbol ps => ps.Type,
				IFieldSymbol fs => fs.Type,
				ILocalSymbol ls => ls.Type,
				_ => null,
			};

			if (symbol is not INamedTypeSymbol
				{
					Arity: 1,
					Name: "Owned",
					ContainingNamespace:
					{
						Name: "Cache",
						ContainingNamespace:
						{
							Name: "Immediate",
							ContainingNamespace.IsGlobalNamespace: true,
						},
					},
				})
			{
				continue;
			}

			context.ReportSuppression(
				Suppression.Create(
					SupportedSuppressions[0],
					diagnostic
				)
			);
		}
	}
}
