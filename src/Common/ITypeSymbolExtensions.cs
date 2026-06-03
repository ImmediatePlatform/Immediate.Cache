using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Immediate.Cache;

internal static class ITypeSymbolExtensions
{
	extension([NotNullWhen(true)] ITypeSymbol? typeSymbol)
	{
		public bool IsImmediateAssemblyIdentifierAttribute =>
			typeSymbol is INamedTypeSymbol
			{
				Arity: 0,
				Name: "ImmediateAssemblyIdentifierAttribute",
				ContainingNamespace.IsImmediateHandlersShared: true,
			};

		public bool IsHandlerAttribute =>
			typeSymbol is INamedTypeSymbol
			{
				Name: "HandlerAttribute",
				ContainingNamespace.IsImmediateHandlersShared: true,
			};
	}

	extension(INamedTypeSymbol typeSymbol)
	{
		public bool GetValidHandleMethod([NotNullWhen(true)] out ITypeSymbol? requestType, [NotNullWhen(true)] out ITypeSymbol? responseType)
		{
			requestType = null;
			responseType = null;

			if (!typeSymbol.GetAttributes().Any(a => a.AttributeClass.IsHandlerAttribute))
				return false;

			if (typeSymbol
					.GetMembers()
					.OfType<IMethodSymbol>()
					.Where(m => m.Name is "Handle" or "HandleAsync")
					.Take(2)
					.ToList() is not [var handleMethod])
			{
				return false;
			}

			// must have request type
			if (handleMethod.Parameters is not [{ Type: ITypeSymbol parameterType }, ..])
				return false;

			if (handleMethod.ReturnType is not INamedTypeSymbol
				{
					Arity: 1,
					Name: "ValueTask",
					ContainingNamespace.IsSystemThreadingTasks: true,
					TypeArguments: [ITypeSymbol returnType],
				})
			{
				return false;
			}

			requestType = parameterType;
			responseType = returnType;
			return true;
		}
	}

	extension(INamespaceSymbol namespaceSymbol)
	{
		public bool IsImmediateCacheShared =>
			namespaceSymbol is
			{
				Name: "Shared",
				ContainingNamespace:
				{
					Name: "Cache",
					ContainingNamespace:
					{
						Name: "Immediate",
						ContainingNamespace.IsGlobalNamespace: true,
					},
				},
			};

		public bool IsImmediateHandlersShared =>
			namespaceSymbol is
			{
				Name: "Shared",
				ContainingNamespace:
				{
					Name: "Handlers",
					ContainingNamespace:
					{
						Name: "Immediate",
						ContainingNamespace.IsGlobalNamespace: true,
					},
				},
			};

		public bool IsSystemThreadingTasks =>
			namespaceSymbol is
			{
				Name: "Tasks",
				ContainingNamespace:
				{
					Name: "Threading",
					ContainingNamespace:
					{
						Name: "System",
						ContainingNamespace.IsGlobalNamespace: true,
					},
				},
			};
	}
}
