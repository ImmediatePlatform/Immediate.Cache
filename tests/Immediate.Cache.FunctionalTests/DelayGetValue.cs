using Immediate.Handlers.Shared;

namespace Immediate.Cache.FunctionalTests;

[Handler]
public static partial class DelayGetValue
{
	public sealed class Query
	{
		public required int Value { get; init; }
		public required string Name { get; init; }
		public required TaskCompletionSource CompletionSource { get; init; }
		public int TimesExecuted { get; set; }
	}

	public sealed record Response(int Value, bool ExecutedHandler, Guid RandomValue);

	private static readonly object s_lock = new();

	private static async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken _
	)
	{
		await query.CompletionSource.Task;
		lock (s_lock)
			query.TimesExecuted++;

		return new(query.Value, ExecutedHandler: true, RandomValue: Guid.NewGuid());
	}
}
