using System.Diagnostics.CodeAnalysis;
using Immediate.Cache.Shared;

namespace Immediate.Cache.FunctionalTests;

[CacheFor<GetValue>]
public sealed partial class GetValueCache
{
	[SuppressMessage(
		"Design",
		"CA1062:Validate arguments of public methods",
		Justification = "Not a public method"
	)]
	protected override string TransformKey(GetValue.Query request) =>
		$"GetValue(query: {request.Value})";
}
