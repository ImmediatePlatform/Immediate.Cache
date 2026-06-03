using static Immediate.Cache.Tests.Utility;

namespace Immediate.Cache.Tests.GeneratorTests;

public sealed class CacheGenerationTests
{
	[Fact]
	public async Task ValidCache()
	{
		var result = GeneratorTestHelper.RunGenerator(
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
		);

		Assert.Equal(
			[
				"Immediate.Cache.Generators/Immediate.Cache.Generators.ImmediateCacheGenerator/IC.Dummy.GetUsersQueryCache.g.cs",
				"Immediate.Cache.Generators/Immediate.Cache.Generators.ImmediateCacheGenerator/IC.ServiceCollectionExtensions.g.cs",
				"Immediate.Handlers.Generators/Immediate.Handlers.Generators.ImmediateHandlersGenerator/IH.Dummy.GetUsersQuery.g.cs",
				"Immediate.Handlers.Generators/Immediate.Handlers.Generators.ImmediateHandlersGenerator/IH.ServiceCollectionExtensions.g.cs",
			],
			result.GeneratedTrees.Select(t => t.FilePath.Replace('\\', '/'))
		);

		_ = await VerifyIgnoreImmediateHandlers(result);
	}

	[Fact]
	public async Task NotHandler_DoesNotGenerate()
	{
		var result = GeneratorTestHelper.RunGenerator(
			"""
			using System.Threading;
			using System.Threading.Tasks;
			using Immediate.Cache.Shared;
			using Immediate.Handlers.Shared;
			
			namespace Dummy;

			[CacheFor<GetUsersQuery>]
			public sealed partial class GetUsersQueryCache
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
			""",
			skippedSteps: ["CacheDefinitions"]
		);

		Assert.Equal(
			[
				"Immediate.Cache.Generators/Immediate.Cache.Generators.ImmediateCacheGenerator/IC.ServiceCollectionExtensions.g.cs",
			],
			result.GeneratedTrees.Select(t => t.FilePath.Replace('\\', '/'))
		);

		_ = await VerifyIgnoreImmediateHandlers(result);
	}

	[Fact]
	public async Task StaticHandler_DoesNotGenerate()
	{
		var result = GeneratorTestHelper.RunGenerator(
			"""
			using System.Threading;
			using System.Threading.Tasks;
			using Immediate.Cache.Shared;
			using Immediate.Handlers.Shared;
			
			namespace Dummy;

			[CacheFor<GetUsersQuery>]
			public sealed partial class GetUsersQueryCache
			{
			}
			
			[Handler]
			public static partial class GetUsersQuery
			{
				public record Query;
				public record Response;
			
				private static async ValueTask<Response> HandleAsync(
					Query _,
					CancellationToken token
				)
				{
					await Task.CompletedTask;
					return new();
				}
			}
			""",
			skippedSteps: ["CacheDefinitions"]
		);

		Assert.Equal(
			[
				"Immediate.Cache.Generators/Immediate.Cache.Generators.ImmediateCacheGenerator/IC.ServiceCollectionExtensions.g.cs",
				"Immediate.Handlers.Generators/Immediate.Handlers.Generators.ImmediateHandlersGenerator/IH.Dummy.GetUsersQuery.g.cs",
				"Immediate.Handlers.Generators/Immediate.Handlers.Generators.ImmediateHandlersGenerator/IH.ServiceCollectionExtensions.g.cs",
			],
			result.GeneratedTrees.Select(t => t.FilePath.Replace('\\', '/'))
		);

		_ = await VerifyIgnoreImmediateHandlers(result);
	}

	[Fact]
	public async Task InvalidHandlerReturn_DoesNotGenerate()
	{
		var result = GeneratorTestHelper.RunGenerator(
			"""
			using System.Threading;
			using System.Threading.Tasks;
			using Immediate.Cache.Shared;
			using Immediate.Handlers.Shared;
			
			namespace Dummy;

			[CacheFor<GetUsersQuery>]
			public sealed partial class GetUsersQueryCache
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
			""",
			skippedSteps: ["CacheDefinitions"]
		);

		Assert.Equal(
			[
				"Immediate.Cache.Generators/Immediate.Cache.Generators.ImmediateCacheGenerator/IC.ServiceCollectionExtensions.g.cs",
				"Immediate.Handlers.Generators/Immediate.Handlers.Generators.ImmediateHandlersGenerator/IH.Dummy.GetUsersQuery.g.cs",
				"Immediate.Handlers.Generators/Immediate.Handlers.Generators.ImmediateHandlersGenerator/IH.ServiceCollectionExtensions.g.cs",
			],
			result.GeneratedTrees.Select(t => t.FilePath.Replace('\\', '/'))
		);

		_ = await VerifyIgnoreImmediateHandlers(result);
	}
}
