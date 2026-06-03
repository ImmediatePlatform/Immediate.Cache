using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Immediate.Cache;

internal static class SyntaxExtensions
{
	extension(MemberDeclarationSyntax mds)
	{
		public bool IsStatic => mds.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
	}
}
