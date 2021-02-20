using EasyCaching.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ZiggyCreatures.Caching.Fusion.Tests
{

	// REMOVE THE abstract MODIFIER TO RUN THESE TESTS
	public abstract class ConcurrentFactoryCallsTests_EasyCaching
	{

		[Theory]
		[InlineData(10)]
		[InlineData(100)]
		[InlineData(1_000)]
		public async Task OnlyOneFactoryGetsCalledEvenInHighConcurrencyAsync(int accessorsCount)
		{
			var services = new ServiceCollection();
			services.AddEasyCaching(options => { options.UseInMemory("default"); });
			var serviceProvider = services.BuildServiceProvider();
			var factory = serviceProvider.GetRequiredService<IEasyCachingProviderFactory>();
			var cache = factory.GetCachingProvider("default");

			var factoryCallsCount = 0;

			var tasks = new ConcurrentBag<Task>();
			Parallel.For(0, accessorsCount, _ =>
			{
				var task = cache.GetAsync<int>(
					"foo",
					async () =>
					{
						Interlocked.Increment(ref factoryCallsCount);
						await Task.Delay(100).ConfigureAwait(false);
						return 42;
					},
					TimeSpan.FromSeconds(10)
				);
				tasks.Add(task);
			});

			await Task.WhenAll(tasks);

			Assert.Equal(1, factoryCallsCount);
		}

		[Theory]
		[InlineData(10)]
		[InlineData(100)]
		[InlineData(1_000)]
		public void OnlyOneFactoryGetsCalledEvenInHighConcurrency(int accessorsCount)
		{
			var services = new ServiceCollection();
			services.AddEasyCaching(options => { options.UseInMemory("default"); });
			var serviceProvider = services.BuildServiceProvider();
			var factory = serviceProvider.GetRequiredService<IEasyCachingProviderFactory>();
			var cache = factory.GetCachingProvider("default");

			var factoryCallsCount = 0;

			Parallel.For(0, accessorsCount, _ =>
			{
				cache.Get<int>(
					"foo",
					() =>
					{
						Interlocked.Increment(ref factoryCallsCount);
						Thread.Sleep(100);
						return 42;
					},
					TimeSpan.FromSeconds(10)
				);
			});

			Assert.Equal(1, factoryCallsCount);
		}

		[Theory]
		[InlineData(10)]
		[InlineData(100)]
		[InlineData(1_000)]
		public async Task OnlyOneFactoryGetsCalledEvenInMixedHighConcurrencyAsync(int accessorsCount)
		{
			var services = new ServiceCollection();
			services.AddEasyCaching(options => { options.UseInMemory("default"); });
			var serviceProvider = services.BuildServiceProvider();
			var factory = serviceProvider.GetRequiredService<IEasyCachingProviderFactory>();
			var cache = factory.GetCachingProvider("default");

			var factoryCallsCount = 0;

			var tasks = new ConcurrentBag<Task>();
			Parallel.For(0, accessorsCount, idx =>
			{
				if (idx % 2 == 0)
				{
					var task = cache.GetAsync<int>(
						"foo",
						async () =>
						{
							Interlocked.Increment(ref factoryCallsCount);
							await Task.Delay(100).ConfigureAwait(false);
							return 42;
						},
						TimeSpan.FromSeconds(10)
					);
					tasks.Add(task);
				}
				else
				{
					cache.Get<int>(
						"foo",
						() =>
						{
							Interlocked.Increment(ref factoryCallsCount);
							Thread.Sleep(100);
							return 42;
						},
						TimeSpan.FromSeconds(10)
					);
				}
			});

			await Task.WhenAll(tasks);

			Assert.Equal(1, factoryCallsCount);
		}
	}

}