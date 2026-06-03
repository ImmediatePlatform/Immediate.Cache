using System.Diagnostics.CodeAnalysis;
using Immediate.Cache.Generators;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Immediate.Cache.Tests.AnalyzerTests;

public static class AnalyzerTestHelpers
{
	public static CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> CreateAnalyzerTest<TAnalyzer>(
		[StringSyntax("c#-test")] string inputSource
	)
		where TAnalyzer : DiagnosticAnalyzer, new()
	{
		var csTest = new ImmediateCacheGeneratorAnalyzerTest<TAnalyzer>
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { inputSource },
				ReferenceAssemblies = Utility.ReferenceAssemblies,
			},
		};

		csTest.TestState.AdditionalReferences
			.AddRange(Utility.GetAdditionalReferences());

		return csTest;
	}

	private sealed class ImmediateCacheGeneratorAnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
		where TAnalyzer : DiagnosticAnalyzer, new()
	{
		protected override IEnumerable<Type> GetSourceGenerators() =>
			[typeof(ImmediateCacheGenerator)];
	}
}
