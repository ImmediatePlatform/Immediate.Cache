using Immediate.Cache.Analyzers;

namespace Immediate.Cache.Tests.AnalyzerTests;

public sealed class CacheForUsageAnalyzerTests
{
	[Fact]
	public async Task ValidContainer_DoesNotAlert() =>
		await AnalyzerTestHelpers.CreateAnalyzerTest<CacheForUsageAnalyzer>(
			"""
			using System.Threading;
			using System.Threading.Tasks;
			using Immediate.Cache.Shared;
			using Immediate.Handlers.Shared;
			
			namespace Dummy;

			[CacheFor<GetUsersQuery>]
			public sealed partial class GetUsersQueryCache
			{
				protected override string TransformKey(GetUsersQuery.Query request) => "Test";
			}
			
			[Handler]
			public sealed partial class GetUsersQuery
			{
				public record Query;
				public record Response;
			
				private async ValueTask<Response> HandleAsync(
					Query _,
					CancellationToken token
				)
				{
					await Task.CompletedTask;
					return new();
				}
			}
			"""
		).RunAsync(TestContext.Current.CancellationToken);

	[Fact]
	public async Task NestedContainer_Alerts() =>
		await AnalyzerTestHelpers.CreateAnalyzerTest<CacheForUsageAnalyzer>(
			"""
			using System.Threading;
			using System.Threading.Tasks;
			using Immediate.Cache.Shared;
			using Immediate.Handlers.Shared;
			
			namespace Dummy;

			public class Outer
			{
				[CacheFor<GetUsersQuery>]
				public sealed partial class {|IC0001:GetUsersQueryCache|}
				{
				}
			}
			
			[Handler]
			public sealed partial class GetUsersQuery
			{
				public record Query;
				public record Response;
			
				private async ValueTask<Response> HandleAsync(
					Query _,
					CancellationToken token
				)
				{
					await Task.CompletedTask;
					return new();
				}
			}
			"""
		).RunAsync(TestContext.Current.CancellationToken);

	[Fact]
	public async Task NestedNonContainer_DoesNotAlert() =>
		await AnalyzerTestHelpers.CreateAnalyzerTest<CacheForUsageAnalyzer>(
			"""
			using System.Threading;
			using System.Threading.Tasks;
			using Immediate.Cache.Shared;
			using Immediate.Handlers.Shared;
			
			namespace Dummy;

			public class Outer
			{
				public sealed partial class GetUsersQueryCache
				{
				}
			}
			
			[Handler]
			public sealed partial class GetUsersQuery
			{
				public record Query;
				public record Response;
			
				private async ValueTask<Response> HandleAsync(
					Query _,
					CancellationToken token
				)
				{
					await Task.CompletedTask;
					return new();
				}
			}
			"""
		).RunAsync(TestContext.Current.CancellationToken);

	[Fact]
	public async Task NonHandlerTarget_Alerts() =>
		await AnalyzerTestHelpers.CreateAnalyzerTest<CacheForUsageAnalyzer>(
			"""
			using System.Threading;
			using System.Threading.Tasks;
			using Immediate.Cache.Shared;
			using Immediate.Handlers.Shared;
			
			namespace Dummy;

			[CacheFor<GetUsersQuery>]
			public sealed partial class {|IC0002:GetUsersQueryCache|}
			{
			}
			
			public sealed partial class GetUsersQuery
			{
				public record Query;
				public record Response;
			
				private async ValueTask<Response> HandleAsync(
					Query _,
					CancellationToken token
				)
				{
					await Task.CompletedTask;
					return new();
				}
			}
			"""
		).RunAsync(TestContext.Current.CancellationToken);

	[Fact]
	public async Task HandlerTargetNoReturnValue_Alerts() =>
		await AnalyzerTestHelpers.CreateAnalyzerTest<CacheForUsageAnalyzer>(
			"""
			using System.Threading;
			using System.Threading.Tasks;
			using Immediate.Cache.Shared;
			using Immediate.Handlers.Shared;
			
			namespace Dummy;

			[CacheFor<GetUsersQuery>]
			public sealed partial class {|IC0003:GetUsersQueryCache|}
			{
			}
			
			[Handler]
			public sealed partial class GetUsersQuery
			{
				public record Query;
				public record Response;
			
				private async ValueTask HandleAsync(
					Query _,
					CancellationToken token
				)
				{
					await Task.CompletedTask;
				}
			}
			"""
		).RunAsync(TestContext.Current.CancellationToken);

}
