using Immediate.Handlers.Shared;

namespace Immediate.Cache.FunctionalTests;

[Handler]
public static partial class DelayGetValue
{
	public sealed class Query
	{
		public required int Value { get; init; }
		public required string Name { get; init; }
		public int TimesExecuted { get; set; }
		public int TimesCancelled { get; set; }
		public CancellationToken CancellationToken { get; set; }

		public TaskCompletionSource WaitForTestToContinueOperation { get; } = new();
		public TaskCompletionSource WaitForTestToStartExecuting { get; } = new();
		public TaskCompletionSource WaitForTestToFinalize { get; } = new();
	}

	public sealed record Response(int Value, bool ExecutedHandler, Guid RandomValue);

	private static readonly Lock s_lock = new();

	private static async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken token
	)
	{
		try
		{
			query.CancellationToken = token;
			_ = query.WaitForTestToStartExecuting.TrySetResult();
			await query.WaitForTestToContinueOperation.Task.WaitAsync(token);

			lock (s_lock)
				query.TimesExecuted++;

			return new(query.Value, ExecutedHandler: true, RandomValue: Guid.NewGuid());
		}
		catch
		{
			lock (s_lock)
				query.TimesCancelled++;
			throw;
		}
		finally
		{
			_ = query.WaitForTestToFinalize.TrySetResult();
		}
	}
}
