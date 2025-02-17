using System.Diagnostics.CodeAnalysis;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace Immediate.Cache.FunctionalTests;

public sealed class DelayGetValueCache(
	IMemoryCache memoryCache,
	Owned<IHandler<DelayGetValue.Query, DelayGetValue.Response>> ownedHandler
) : ApplicationCacheBase<DelayGetValue.Query, DelayGetValue.Response>(
	memoryCache,
	ownedHandler
)
{
	[SuppressMessage(
		"Design",
		"CA1062:Validate arguments of public methods",
		Justification = "Not a public method"
	)]
	protected override string TransformKey(DelayGetValue.Query request) =>
		$"DelayGetValue(query: {request.Value})";

	public ValueTask<DelayGetValue.Response> TransformResult(DelayGetValue.Query query, TransformParameters transformation)
	{
		return TransformValue(
			query,
			async (r, ct) =>
			{
				_ = transformation.WaitForTestToStartExecuting.TrySetResult();
				await transformation.WaitForTestToContinueOperation.Task;

				transformation.TimesExecuted++;

				return r with { Value = r.Value + transformation.Adder };
			},
			default
		);
	}

	public sealed class TransformParameters
	{
		public required int Adder { get; init; }
		public int TimesExecuted { get; set; }
		public TaskCompletionSource WaitForTestToStartExecuting { get; } = new();
		public TaskCompletionSource WaitForTestToContinueOperation { get; } = new();
	}
}
