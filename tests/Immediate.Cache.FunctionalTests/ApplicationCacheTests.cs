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
}
