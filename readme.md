# Immediate.Cache

[![NuGet](https://img.shields.io/nuget/v/Immediate.Cache.svg?style=plastic)](https://www.nuget.org/packages/Immediate.Cache/)
[![GitHub release](https://img.shields.io/github/release/ImmediatePlatform/Immediate.Cache.svg)](https://GitHub.com/ImmediatePlatform/Immediate.Cache/releases/)
[![GitHub license](https://img.shields.io/github/license/ImmediatePlatform/Immediate.Cache.svg)](https://github.com/ImmediatePlatform/Immediate.Cache/blob/master/license.txt) 
[![GitHub issues](https://img.shields.io/github/issues/ImmediatePlatform/Immediate.Cache.svg)](https://GitHub.com/ImmediatePlatform/Immediate.Cache/issues/) 
[![GitHub issues-closed](https://img.shields.io/github/issues-closed/ImmediatePlatform/Immediate.Cache.svg)](https://GitHub.com/ImmediatePlatform/Immediate.Cache/issues?q=is%3Aissue+is%3Aclosed) 
[![GitHub Actions](https://github.com/ImmediatePlatform/Immediate.Cache/actions/workflows/build.yml/badge.svg)](https://github.com/ImmediatePlatform/Immediate.Cache/actions)
---

Immediate.Cache is a collection of classes that simplify caching responses from [Immediate.Handlers](https://github.com/ImmediatePlatform/Immediate.Handlers) handlers.

## Installing Immediate.Cache

You can install [Immediate.Cache with NuGet](https://www.nuget.org/packages/Immediate.Cache):

    Install-Package Immediate.Cache
    
Or via the .NET Core command line interface:

    dotnet add package Immediate.Cache

Either commands, from Package Manager Console or .NET Core CLI, will download and install Immediate.Cache.

## Using Immediate.Cache

### Creating a Cache

Create a subclass of `ApplicationCacheBase`, which will serve as the cache for a particular handler. An example:
```cs
[Handler]
public static partial class GetValue
{
	public sealed record Query(int Value);
	public sealed record Response(int Value);

	private static ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken _
	) => ValueTask.FromResult(new Response(query.Value));
}

public sealed class GetValueCache(
	IMemoryCache memoryCache,
	Owned<IHandler<GetValue.Query, GetValue.Response>> ownedHandler
) : ApplicationCacheBase<GetValue.Query, GetValue.Response>(
	memoryCache,
	ownedHandler
)
{
	protected override string TransformKey(GetValue.Query request) =>
		$"GetValue(query: {request.Value})";
}
```

In this case, the `GetValueCache` class will serve as a cache for the `GetValue` IH handler.

### Register the Cache with DI

In your `Program.cs` file:

* Ensure that Memory Cache is registered, by calling:
```cs
services.AddMemoryCache();
```

* Register `Owned<>` as a singleton
```cs
services.AddSingleton(typeof(Owned<>));
```

* Register your cache service(s) as a singleton(s)
```cs
services.AddSingleton<GetValueCache>();
```

### Retrieve Data From the Cache

Using an instance of the `GetValueCache` class that you have created above, you can simply call:
```cs
var response = await cache.GetValue(request, token);
```

If there is a cached value, it will be returned; otherwise a temporary scope will be used to create the handler and
execute it; and the returned value will be stored.

> [!NOTE]
> If simultaneous requests are made while the handler is executing, they will wait for the first handler to
complete, rather than executing the handler a second/simultaenous time.

### Removing Data From the Cache

Using an instance of the `GetValueCache` class that you have created above, you can remove cached data by calling:
```cs
await cache.RemoveValue(request);
```

> [!NOTE]
> If a handler is running based on this request, it will be cancelled, and any callers waiting on the results from 
> this handler will experience a `CancellationToken` cancellation.

### Updating Data In the Cache

Using an instance of the `GetValueCache` class that you have created above, you can assign cached data by calling:
```cs
await cache.SetValue(request, response);
```

> [!NOTE]
> If a handler is running based on this request, it will be cancelled, and any callers waiting on the results from 
> this handler will immediately receive the updated response.
