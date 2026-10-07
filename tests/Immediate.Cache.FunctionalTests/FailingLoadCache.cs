using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;

namespace Immediate.Cache.FunctionalTests;

[Handler]
public sealed partial class FailingLoad
{
	public const string FailingLoadMessage = "load failed";

	public sealed class Query
	{
		public required string Key { get; init; }
		public TaskCompletionSource Started { get; } = new();
		public TaskCompletionSource Release { get; } = new();
	}

	private async ValueTask<string> HandleAsync(Query query, CancellationToken _)
	{
		query.Started.TrySetResult();

		// Stands in for work that fails without observing the token, such as a database query whose rows changed underneath it.
		await query.Release.Task;

		throw new FailingLoadException(FailingLoadMessage);
	}

	public sealed class FailingLoadException : Exception
	{
		public FailingLoadException()
		{
		}

		public FailingLoadException(string message) : base(message)
		{
		}

		public FailingLoadException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}

[CacheFor<FailingLoad>]
public sealed partial class FailingLoadCache
{
	protected override string TransformKey(FailingLoad.Query request) => request.Key;
}
