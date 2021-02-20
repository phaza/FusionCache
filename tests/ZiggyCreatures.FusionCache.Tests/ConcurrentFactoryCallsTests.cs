using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ZiggyCreatures.Caching.Fusion.Tests
{

	public class ConcurrentFactoryCallsTests
	{

		[Theory]
		[InlineData(10)]
		[InlineData(100)]
		[InlineData(1_000)]
		public async Task OnlyOneFactoryGetsCalledEvenInHighConcurrencyAsync(int accessorsCount)
		{
			using (var cache = new FusionCache(new FusionCacheOptions()))
			{
				var factoryCallsCount = 0;

				var tasks = new ConcurrentBag<Task>();
				Parallel.For(0, accessorsCount, _ =>
				{
					var task = cache.GetOrSetAsync<int>(
						"foo",
						async _ =>
						{
							Interlocked.Increment(ref factoryCallsCount);
							await Task.Delay(100).ConfigureAwait(false);
							return 42;
						},
						new FusionCacheEntryOptions(TimeSpan.FromSeconds(10))
					);
					tasks.Add(task);
				});

				await Task.WhenAll(tasks);

				Assert.Equal(1, factoryCallsCount);
			}
		}

		[Theory]
		[InlineData(10)]
		[InlineData(100)]
		[InlineData(1_000)]
		public void OnlyOneFactoryGetsCalledEvenInHighConcurrency(int accessorsCount)
		{
			using (var cache = new FusionCache(new FusionCacheOptions()))
			{
				var factoryCallsCount = 0;

				Parallel.For(0, accessorsCount, _ =>
				{
					cache.GetOrSet<int>(
						"foo",
						_ =>
						{
							Interlocked.Increment(ref factoryCallsCount);
							Thread.Sleep(100);
							return 42;
						},
						new FusionCacheEntryOptions(TimeSpan.FromSeconds(10))
					);
				});

				Assert.Equal(1, factoryCallsCount);
			}
		}

		[Theory]
		[InlineData(10)]
		[InlineData(100)]
		[InlineData(1_000)]
		public async Task OnlyOneFactoryGetsCalledEvenInMixedHighConcurrencyAsync(int accessorsCount)
		{
			using (var cache = new FusionCache(new FusionCacheOptions()))
			{
				var factoryCallsCount = 0;

				var tasks = new ConcurrentBag<Task>();
				Parallel.For(0, accessorsCount, idx =>
				{
					if (idx % 2 == 0)
					{
						var task = cache.GetOrSetAsync<int>(
							"foo",
							async _ =>
							{
								Interlocked.Increment(ref factoryCallsCount);
								await Task.Delay(100).ConfigureAwait(false);
								return 42;
							},
							new FusionCacheEntryOptions(TimeSpan.FromSeconds(10))
						);
						tasks.Add(task);
					}
					else
					{
						cache.GetOrSet<int>(
							"foo",
							_ =>
							{
								Interlocked.Increment(ref factoryCallsCount);
								Thread.Sleep(100);
								return 42;
							},
							new FusionCacheEntryOptions(TimeSpan.FromSeconds(10))
						);
					}
				});

				await Task.WhenAll(tasks);

				Assert.Equal(1, factoryCallsCount);
			}
		}

	}

}