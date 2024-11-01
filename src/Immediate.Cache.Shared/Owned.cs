using Microsoft.Extensions.DependencyInjection;

namespace Immediate.Cache;

/// <summary>
///		A factory for creating a scope containing a strong-type service as it's root.
/// </summary>
/// <typeparam name="T">
///		The type of the service that should be created at the root of the scope.
/// </typeparam>
/// <param name="serviceScopeFactory">
///		A <see cref="IServiceScopeFactory"/> used to create the scope for the service.
/// </param>
public sealed class Owned<T>(
	IServiceScopeFactory serviceScopeFactory
) where T : class
{
	/// <summary>
	///	    Creates a temporary scope and gets an instance of the service from that scope.
	/// </summary>
	/// <returns>
	///	    An <see cref="OwnedScope{T}"/> containing both the scope and the service, so that the scope can be disposed
	///     at the appropriate time.
	/// </returns>
	public OwnedScope<T> GetScope()
	{
		var scope = serviceScopeFactory.CreateAsyncScope();
		return new(
			scope.ServiceProvider.GetRequiredService<T>(),
			scope
		);
	}
}
