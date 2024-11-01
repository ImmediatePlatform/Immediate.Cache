namespace Immediate.Cache;

/// <summary>
///	    Represents a container for a scope and a scoped service that is rooted by the scope.
/// </summary>
/// <typeparam name="T">
///	    The type of the service contained by the scope.
/// </typeparam>
public sealed class OwnedScope<T> : IAsyncDisposable
{
	internal OwnedScope(
		T service,
		IAsyncDisposable disposable
	)
	{
		Service = service;
		_disposable = disposable;
	}

	/// <summary>
	///	    The instance of the service contained by the scope.
	/// </summary>
	public T Service { get; }

	private readonly IAsyncDisposable _disposable;

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await _disposable.DisposeAsync().ConfigureAwait(false);
	}
}
