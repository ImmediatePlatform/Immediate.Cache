using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;

namespace Immediate.Cache.FunctionalTests;

[CacheFor<NullableTypesHandler>]
public sealed partial class NullableTypesCache
{
	protected override string TransformKey(NullableTypesHandler.Query? request) =>
		$"NullableTypesHandler(query: {request})";
}

[Handler]
public sealed partial class NullableTypesHandler
{
	public sealed record Query;
	public sealed record Response;

	private async ValueTask<Response?> HandleAsync(Query? _, CancellationToken __)
	{
		return null;
	}
}
