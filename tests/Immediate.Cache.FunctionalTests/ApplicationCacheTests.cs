using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Immediate.Cache.FunctionalTests;

public sealed class ApplicationCacheTests
{
	private readonly IServiceProvider _serviceProvider;

	public ApplicationCacheTests()
	{
		var services = new ServiceCollection();
		_ = services.AddHandlers();
		_ = services.AddSingleton<GetValueCache>();
		_ = services.AddSingleton<DelayGetValueCache>();
		_ = services.AddSingleton(typeof(Owned<>));
		_ = services.AddMemoryCache();

		_serviceProvider = services.BuildServiceProvider();
	}

	[Test]
	public async Task GetValueCachesValue()
	{
		var request = new GetValue.Query(Value: 1);
		var cache = _serviceProvider.GetRequiredService<GetValueCache>();
		var response = await cache.GetValue(request);

		Assert.Equal(1, response.Value);
		Assert.True(response.ExecutedHandler);

		var memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
		Assert.True(memoryCache.TryGetValue($"GetValue(query: {request.Value})", out var _));
	}

	[Test]
	public async Task SetValueCachesValue()
	{
		var request = new GetValue.Query(Value: 2);
		var cache = _serviceProvider.GetRequiredService<GetValueCache>();
		cache.SetValue(request, new(4, ExecutedHandler: false));

		var memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
		Assert.True(memoryCache.TryGetValue($"GetValue(query: {request.Value})", out var _));

		var response = await cache.GetValue(request);

		Assert.Equal(4, response.Value);
		Assert.False(response.ExecutedHandler);
	}

	[Test]
	public async Task RemoveValueRemovesValue()
	{
		var request = new GetValue.Query(Value: 3);
		var cache = _serviceProvider.GetRequiredService<GetValueCache>();
		cache.SetValue(request, new(4, ExecutedHandler: false));

		var memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
		Assert.True(memoryCache.TryGetValue($"GetValue(query: {request.Value})", out var _));

		var response = await cache.GetValue(request);

		Assert.Equal(4, response.Value);
		Assert.False(response.ExecutedHandler);

		cache.RemoveValue(request);

		response = await cache.GetValue(request);

		Assert.Equal(3, response.Value);
		Assert.True(response.ExecutedHandler);
	}

	[Test]
	public async Task SimultaneousAccessIsSerialized()
	{
		var request1 = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request1",
			CompletionSource = new(),
		};

		var request2 = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request2",
			CompletionSource = new(),
		};

		var cache = _serviceProvider.GetRequiredService<DelayGetValueCache>();
		var response1Task = cache.GetValue(request1);
		var response2Task = cache.GetValue(request2);

		// both waiting until tcs triggered
		Assert.False(response1Task.IsCompleted);
		Assert.False(response2Task.IsCompleted);

		Assert.Equal(0, request1.TimesExecuted);
		Assert.Equal(0, request2.TimesExecuted);

		// request2 does nothing at this point
		request2.CompletionSource.SetResult();

		Assert.False(response1Task.IsCompleted);
		Assert.False(response2Task.IsCompleted);

		Assert.Equal(0, request1.TimesExecuted);
		Assert.Equal(0, request2.TimesExecuted);

		// trigger request1, which should run exactly once
		request1.CompletionSource.SetResult();

		var response1 = await response1Task;
		var response2 = await response2Task;

		Assert.Equal(1, request1.TimesExecuted);
		Assert.Equal(0, request2.TimesExecuted);

		// ensure both responses get the same response back
		Assert.Equal(response1.RandomValue, response2.RandomValue);
	}
}
