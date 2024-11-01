using Immediate.Handlers.Shared;

namespace Immediate.Cache.FunctionalTests;

[Handler]
public static partial class GetValue
{
	public sealed record Query(int Value);
	public sealed record Response(int Value, bool ExecutedHandler);

	private static ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken _
	) => ValueTask.FromResult(new Response(query.Value, ExecutedHandler: true));
}
