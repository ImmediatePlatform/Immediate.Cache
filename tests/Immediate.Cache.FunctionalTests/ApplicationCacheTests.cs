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
		_ = services.AddImmediateCacheFunctionalTestsHandlers();
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
		};

		var request2 = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request2",
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
		request2.WaitForTestToContinueOperation.SetResult();

		Assert.False(response1Task.IsCompleted);
		Assert.False(response2Task.IsCompleted);

		Assert.Equal(0, request1.TimesExecuted);
		Assert.Equal(0, request2.TimesExecuted);

		// trigger request1, which should run exactly once
		request1.WaitForTestToContinueOperation.SetResult();

		var response1 = await response1Task;
		var response2 = await response2Task;

		Assert.Equal(1, request1.TimesExecuted);
		Assert.Equal(0, request2.TimesExecuted);

		// ensure both responses get the same response back
		Assert.Equal(response1.RandomValue, response2.RandomValue);
	}

	[Test]
	public async Task ProperlyUsesCancellationToken()
	{
		var request = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request1",
		};

		using var tcs = new CancellationTokenSource();
		var cache = _serviceProvider.GetRequiredService<DelayGetValueCache>();
		var responseTask = cache.GetValue(request, tcs.Token);

		await tcs.CancelAsync();

		Assert.True(responseTask.IsCanceled);
		Assert.Equal(0, request.TimesExecuted);
		Assert.False(request.CancellationToken.IsCancellationRequested);

		// actual handler will continue executing in spite of no remaining callers
		request.WaitForTestToContinueOperation.SetResult();

		// check that value is now properly in cache
		var request2 = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request2",
		};

		var response = await cache.GetValue(request2);

		Assert.Equal(1, request.TimesExecuted);
		Assert.Equal(0, request2.TimesExecuted);
	}

	[Test]
	public async Task CancellingFirstAccessOperatesCorrectly()
	{
		using var cts1 = new CancellationTokenSource();
		var request1 = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request1",
		};

		using var cts2 = new CancellationTokenSource();
		var request2 = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request2",
		};

		var cache = _serviceProvider.GetRequiredService<DelayGetValueCache>();
		var response1Task = cache.GetValue(request1, cts1.Token);
		var response2Task = cache.GetValue(request2, cts2.Token);

		// both waiting until cancellation triggered
		Assert.False(response1Task.IsCompleted);
		Assert.False(response2Task.IsCompleted);

		Assert.Equal(0, request1.TimesExecuted);
		Assert.Equal(0, request2.TimesExecuted);

		// cancel query1; query2 should remain uncancelled
		await cts1.CancelAsync();

		Assert.True(response1Task.IsCanceled);
		Assert.False(response2Task.IsCanceled);

		await cts2.CancelAsync();

		Assert.True(response1Task.IsCanceled);
		Assert.True(response2Task.IsCanceled);
	}

	[Test]
	public async Task CancellingSecondAccessOperatesCorrectly()
	{
		using var cts1 = new CancellationTokenSource();
		var request1 = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request1",
		};

		using var cts2 = new CancellationTokenSource();
		var request2 = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request2",
		};

		var cache = _serviceProvider.GetRequiredService<DelayGetValueCache>();
		var response1Task = cache.GetValue(request1, cts1.Token);
		var response2Task = cache.GetValue(request2, cts2.Token);

		// both waiting until cancellation triggered
		Assert.False(response1Task.IsCompleted);
		Assert.False(response2Task.IsCompleted);

		Assert.Equal(0, request1.TimesExecuted);
		Assert.Equal(0, request2.TimesExecuted);

		// cancel query2; query1 should remain uncancelled
		await cts2.CancelAsync();

		Assert.False(response1Task.IsCanceled);
		Assert.True(response2Task.IsCanceled);

		await cts1.CancelAsync();

		Assert.True(response1Task.IsCanceled);
		Assert.True(response2Task.IsCanceled);
	}

	[Test]
	public async Task RemovingValueCancelsExistingOperation()
	{
		var request = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request1",
		};

		using var tcs = new CancellationTokenSource();
		var cache = _serviceProvider.GetRequiredService<DelayGetValueCache>();
		var responseTask = cache.GetValue(request, tcs.Token);

		await request.WaitForTestToStartExecuting.Task;

		cache.RemoveValue(request);
		await request.WaitForTestToFinalize.Task;

		request.WaitForTestToContinueOperation.SetResult();

		var response = await responseTask;

		Assert.Equal(1, request.TimesExecuted);
		Assert.Equal(1, request.TimesCancelled);

		Assert.Equal(1, response.Value);
		Assert.True(response.ExecutedHandler);
	}

	[Test]
	public async Task SettingValueCancelsExistingOperation()
	{
		var request = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request1",
		};

		var cache = _serviceProvider.GetRequiredService<DelayGetValueCache>();
		var responseTask = cache.GetValue(request, default);

		await request.WaitForTestToStartExecuting.Task;

		cache.SetValue(request, new(5, ExecutedHandler: false, Guid.NewGuid()));

		var response = await responseTask;
		Assert.Equal(5, response.Value);
		Assert.False(response.ExecutedHandler);

		await request.WaitForTestToFinalize.Task;

		Assert.Equal(0, request.TimesExecuted);
		Assert.Equal(1, request.TimesCancelled);
	}

	[Test]
	public async Task ExceptionGetsPropagatedCorrectly()
	{
		var request = new DelayGetValue.Query()
		{
			Value = 1,
			Name = "Request1",
			ThrowException = true,
		};

		var cache = _serviceProvider.GetRequiredService<DelayGetValueCache>();
		var responseTask = cache.GetValue(request, default);

		var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await responseTask);
		Assert.Equal("Test Exception 1", ex.Message);
	}
}
