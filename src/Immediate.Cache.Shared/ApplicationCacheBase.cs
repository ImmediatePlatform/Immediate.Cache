using System.Diagnostics.CodeAnalysis;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace Immediate.Cache;

/// <summary>
///		Base class for caching the results of an <see cref="IHandler{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">
///		The type of the handler request
/// </typeparam>
/// <typeparam name="TResponse">
///		The type of the handler response
/// </typeparam>
/// <param name="memoryCache">
///		An in-memory cache in which the result of a handler can be stored
/// </param>
/// <param name="handler">
///		The handler from which to cache data
/// </param>
public abstract class ApplicationCacheBase<TRequest, TResponse>(
	IMemoryCache memoryCache,
	Owned<IHandler<TRequest, TResponse>> handler
)
	where TRequest : class
	where TResponse : class
{
	private readonly Lock _lock = new();

	/// <summary>
	///	    Transforms a <typeparamref name="TRequest"/> into a cache entry key.
	/// </summary>
	/// <param name="request">
	///	    The request being made to the handler.
	/// </param>
	/// <returns>
	///	    A <see langword="string" /> used as the key for storing the data in the cache.
	/// </returns>
	protected abstract string TransformKey(TRequest request);

	/// <summary>
	///	    Optionally set <see cref="MemoryCacheEntryOptions"/> for the cache entry
	/// </summary>
	/// <returns>
	///	    A <see cref="MemoryCacheEntryOptions"/> that contains configuration values for the cache entry.
	/// </returns>
	/// <remarks>
	///	    By default, this method sets the <see cref="MemoryCacheEntryOptions.SlidingExpiration"/> to a period of 5
	///     minutes.
	/// </remarks>
	protected virtual MemoryCacheEntryOptions GetCacheEntryOptions() =>
		new()
		{
			SlidingExpiration = TimeSpan.FromMinutes(5),
		};

	private CacheValue GetCacheValue(TRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);

		var key = TransformKey(request);

		if (!memoryCache.TryGetValue(key, out var result))
		{
			lock (_lock)
			{
				if (!memoryCache.TryGetValue(key, out result))
				{
					using var entry = memoryCache.CreateEntry(key)
						.SetOptions(GetCacheEntryOptions());

					result = new CacheValue(request, handler);
					entry.Value = result;
				}
			}
		}

		return (CacheValue)result!;
	}

	/// <summary>
	///	    Retrieves a value from the cache, based on the <paramref name="request"/>. Executes the handler inside of a
	///	    temporary scope if the data is not currently available in the cache.
	/// </summary>
	/// <param name="request">
	///		The request payload to be cached.
	/// </param>
	/// <param name="cancellationToken">
	///		The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
	/// </param>
	/// <returns>
	///		The response payload from executing the handler.
	/// </returns>
	public ValueTask<TResponse> GetValue(TRequest request, CancellationToken cancellationToken = default) =>
		GetCacheValue(request).GetValue(cancellationToken);

	/// <summary>
	///		Sets the value for a particular cache entry, bypassing the execution of the handler.
	/// </summary>
	/// <param name="request">
	///		The request payload to be cached.
	/// </param>
	/// <param name="value">
	///		The response payload to be cached.
	/// </param>
	public void SetValue(TRequest request, TResponse value) =>
		GetCacheValue(request).SetValue(value);

	/// <summary>
	///	    Removes the cached payload for a particular cache entry, forcing future requests for the same request to
	///     execute the handler.
	/// </summary>
	/// <param name="request">
	///		The request payload to be cached.
	/// </param>
	public void RemoveValue(TRequest request) =>
		GetCacheValue(request).RemoveValue();

	[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "CancellationTokenSource does not need to be disposed here.")]
	private sealed class CacheValue(
		TRequest request,
		Owned<IHandler<TRequest, TResponse>> handler
	)
	{
		private CancellationTokenSource? _tokenSource;
		private TaskCompletionSource<TResponse>? _responseSource;
		private readonly Lock _lock = new();

		public async ValueTask<TResponse> GetValue(CancellationToken cancellationToken) =>
			await GetHandlerTask().WaitAsync(cancellationToken).ConfigureAwait(false);

		[SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "Double-checked lock pattern")]
		private Task<TResponse> GetHandlerTask()
		{
			if (_responseSource is { Task.Status: not (TaskStatus.Faulted or TaskStatus.Canceled) })
				return _responseSource.Task;

			lock (_lock)
			{
				if (_responseSource is { Task.Status: not (TaskStatus.Faulted or TaskStatus.Canceled) })
					return _responseSource.Task;

				// escape current sync context
				_ = Task.Factory.StartNew(
					RunHandler,
					CancellationToken.None,
					TaskCreationOptions.PreferFairness,
					TaskScheduler.Current
				);

				return (_responseSource = new()).Task;
			}
		}

		private async Task RunHandler()
		{
			while (true)
			{
				CancellationTokenSource tokenSource;

				lock (_lock)
				{
					if (_responseSource?.Task is { IsCompletedSuccessfully: true })
						return;

					if (_tokenSource is null or { IsCancellationRequested: true })
						_tokenSource = new();

					tokenSource = _tokenSource;
				}

				try
				{
					var token = tokenSource.Token;
					var scope = handler.GetScope();

					await using (scope.ConfigureAwait(false))
					{
						var response = await scope.Service
							.HandleAsync(
								request,
								token
							)
							.ConfigureAwait(false);

						lock (_lock)
						{
							if (!tokenSource.IsCancellationRequested)
							{
								_responseSource!.SetResult(response);
							}
						}
					}
				}
				catch (OperationCanceledException) when (tokenSource.IsCancellationRequested)
				{
				}
			}
		}

		public void SetValue(TResponse response)
		{
			lock (_lock)
			{
				if (_responseSource is null or { Task.IsCompleted: true })
					_responseSource = new TaskCompletionSource<TResponse>();

				_responseSource.SetResult(response);

				_tokenSource?.Cancel();
				_tokenSource = null;
			}
		}

		public void RemoveValue()
		{
			lock (_lock)
			{
				if (_responseSource is { Task.IsCompleted: true })
					_responseSource = null;

				_tokenSource?.Cancel();
				_tokenSource = null;
			}
		}
	}
}
