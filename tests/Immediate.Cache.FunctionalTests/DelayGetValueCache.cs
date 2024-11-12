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
		$"GetValue(query: {request.Value})";
}
