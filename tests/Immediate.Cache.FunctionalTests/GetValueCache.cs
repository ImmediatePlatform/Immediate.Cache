using System.Diagnostics.CodeAnalysis;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace Immediate.Cache.FunctionalTests;

public sealed class GetValueCache(
	IMemoryCache memoryCache,
	Owned<IHandler<GetValue.Query, GetValue.Response>> ownedHandler
) : ApplicationCacheBase<GetValue.Query, GetValue.Response>(
	memoryCache,
	ownedHandler
)
{
	[SuppressMessage(
		"Design",
		"CA1062:Validate arguments of public methods",
		Justification = "Not a public method"
	)]
	protected override string TransformKey(GetValue.Query request) =>
		$"GetValue(query: {request.Value})";
}
