using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;

namespace Immediate.Cache.FunctionalTests;

[Handler]
public sealed partial class GetValue
{
	public sealed record Query(int Value);
	public sealed record Response(int Value, bool ExecutedHandler);

	private ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken _
	) => ValueTask.FromResult(new Response(query.Value, ExecutedHandler: true));
}

[CacheFor<GetValue>]
public sealed partial class GetValueCache
{
	protected override string TransformKey(GetValue.Query request) =>
		$"GetValue(query: {request.Value})";
}
