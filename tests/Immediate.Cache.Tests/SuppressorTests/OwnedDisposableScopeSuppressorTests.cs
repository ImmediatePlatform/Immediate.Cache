using Immediate.Cache.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.NetCore.Analyzers.Runtime;

namespace Immediate.Cache.Tests.SuppressorTests;

public sealed class OwnedDisposableScopeSuppressorTests
{
	public static readonly DiagnosticResult CA2000 =
		DiagnosticResult.CompilerWarning("CA2000");

	[Fact]
	public async Task OutArgumentFromGetScopeAsParameterIsSuppressed() =>
		await SuppressorTestHelpers
			.CreateSuppressorTest<OwnedDisposableScopeSuppressor, DisposeObjectsBeforeLosingScope>(
				"""
				#nullable enable

				using System;
				using System.Threading.Tasks;
				using Immediate.Cache;

				public sealed class Disposable : IDisposable, IAsyncDisposable
				{
					public void Dispose() { }
					public ValueTask DisposeAsync() => default;
				}

				public sealed class Dummy
				{
					public async ValueTask Method(Owned<Disposable> owned)
					{
						await using var scope = owned.GetScope({|#0:out var service|});
					}
				}
				"""
			)
			.WithSpecificDiagnostics([CA2000])
			.WithExpectedDiagnosticsResults([
				CA2000.WithLocation(0).WithIsSuppressed(true),
			])
			.RunAsync(TestContext.Current.CancellationToken);

	[Fact]
	public async Task OutArgumentFromGetScopeAsPropertyIsSuppressed() =>
		await SuppressorTestHelpers
			.CreateSuppressorTest<OwnedDisposableScopeSuppressor, DisposeObjectsBeforeLosingScope>(
				"""
					#nullable enable

					using System;
					using System.Threading.Tasks;
					using Immediate.Cache;

					public sealed class Disposable : IDisposable, IAsyncDisposable
					{
						public void Dispose() { }
						public ValueTask DisposeAsync() => default;
					}

					public sealed class Dummy
					{
						private Owned<Disposable> Owned { get; } = default!;

						public async ValueTask Method()
						{
							await using var scope = Owned.GetScope({|#0:out var service|});
						}
					}
					"""
			)
			.WithSpecificDiagnostics([CA2000])
			.WithExpectedDiagnosticsResults([
				CA2000.WithLocation(0).WithIsSuppressed(true),
			])
			.RunAsync(TestContext.Current.CancellationToken);

	[Fact]
	public async Task OutArgumentFromGetScopeAsFieldIsSuppressed() =>
		await SuppressorTestHelpers
			.CreateSuppressorTest<OwnedDisposableScopeSuppressor, DisposeObjectsBeforeLosingScope>(
				"""
					#nullable enable

					using System;
					using System.Threading.Tasks;
					using Immediate.Cache;

					public sealed class Disposable : IDisposable, IAsyncDisposable
					{
						public void Dispose() { }
						public ValueTask DisposeAsync() => default;
					}

					public sealed class Dummy
					{
						private readonly Owned<Disposable> owned = default!;

						public async ValueTask Method()
						{
							await using var scope = owned.GetScope({|#0:out var service|});
						}
					}
					"""
			)
			.WithSpecificDiagnostics([CA2000])
			.WithExpectedDiagnosticsResults([
				CA2000.WithLocation(0).WithIsSuppressed(true),
			])
			.RunAsync(TestContext.Current.CancellationToken);

	[Fact]
	public async Task OutArgumentFromGetScopeAsVariableIsSuppressed() =>
		await SuppressorTestHelpers
			.CreateSuppressorTest<OwnedDisposableScopeSuppressor, DisposeObjectsBeforeLosingScope>(
				"""
					#nullable enable

					using System;
					using System.Threading.Tasks;
					using Immediate.Cache;

					public sealed class Disposable : IDisposable, IAsyncDisposable
					{
						public void Dispose() { }
						public ValueTask DisposeAsync() => default;
					}

					public sealed class Dummy
					{
						public async ValueTask Method()
						{
							Owned<Disposable> owned = default!;
							await using var scope = owned.GetScope({|#0:out var service|});
						}
					}
					"""
			)
			.WithSpecificDiagnostics([CA2000])
			.WithExpectedDiagnosticsResults([
				CA2000.WithLocation(0).WithIsSuppressed(true),
			])
			.RunAsync(TestContext.Current.CancellationToken);

	[Fact]
	public async Task OutArgumentFromGeneralMethodIsNotSuppressed() =>
		await SuppressorTestHelpers
			.CreateSuppressorTest<OwnedDisposableScopeSuppressor, DisposeObjectsBeforeLosingScope>(
				"""
				#nullable enable

				using System;
				using System.Threading.Tasks;
				using Immediate.Cache;

				public sealed class Disposable : IDisposable, IAsyncDisposable
				{
					public void Dispose() { }
					public ValueTask DisposeAsync() => default;
				}
			
				public sealed class Dummy
				{
					public async ValueTask Method()
					{
						await using var scope = GetValue({|#0:out var service|});
					}

					private IAsyncDisposable GetValue(out Disposable disposable)
					{
						disposable = new();
						return new Disposable();
					}
				}
				"""
			)
			.WithSpecificDiagnostics([CA2000])
			.WithExpectedDiagnosticsResults([
				CA2000.WithLocation(0).WithIsSuppressed(false),
			])
			.RunAsync(TestContext.Current.CancellationToken);

	[Fact]
	public async Task ReturnValueFromGeneralMethodIsNotSuppressed() =>
		await SuppressorTestHelpers
			.CreateSuppressorTest<OwnedDisposableScopeSuppressor, DisposeObjectsBeforeLosingScope>(
				"""
				#nullable enable

				using System;
				using System.Threading.Tasks;
				using Immediate.Cache;

				public sealed class Disposable : IDisposable, IAsyncDisposable
				{
					public void Dispose() { }
					public ValueTask DisposeAsync() => default;
				}
			
				public sealed class Dummy
				{
					public void Method()
					{
						var scope = {|#0:GetValue()|};
					}

					private IDisposable GetValue()
					{
						return new Disposable();
					}
				}
				"""
			)
			.WithSpecificDiagnostics([CA2000])
			.WithExpectedDiagnosticsResults([
				CA2000.WithLocation(0).WithIsSuppressed(false),
			])
			.RunAsync(TestContext.Current.CancellationToken);

	[Fact]
	public async Task NewValueIsNotSuppressed() =>
		await SuppressorTestHelpers
			.CreateSuppressorTest<OwnedDisposableScopeSuppressor, DisposeObjectsBeforeLosingScope>(
				"""
				#nullable enable

				using System;
				using System.Threading.Tasks;
				using Immediate.Cache;

				public sealed class Disposable : IDisposable, IAsyncDisposable
				{
					public void Dispose() { }
					public ValueTask DisposeAsync() => default;
				}
			
				public sealed class Dummy
				{
					public void Method()
					{
						var service = {|#0:new Disposable()|};
					}
				}
				"""
			)
			.WithSpecificDiagnostics([CA2000])
			.WithExpectedDiagnosticsResults([
				CA2000.WithLocation(0).WithIsSuppressed(false),
			])
			.RunAsync(TestContext.Current.CancellationToken);
}
