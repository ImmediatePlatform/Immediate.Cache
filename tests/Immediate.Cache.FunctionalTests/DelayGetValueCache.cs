using System.Diagnostics.CodeAnalysis;
using Immediate.Cache.Shared;

namespace Immediate.Cache.FunctionalTests;

[CacheFor<DelayGetValue>]
public sealed partial class DelayGetValueCache
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
