using Immediate.Handlers.Shared;

namespace Immediate.Cache.Shared;

/// <summary>
///		Apply to a <see cref="HandlerAttribute"/>-attributed class to generate
///		a cache wrapper for the handler.
/// </summary>
/// <typeparam name="THandler">
///		The handler class for which to generate a cache.
/// </typeparam>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheForAttribute<THandler> : Attribute
	where THandler : class;
