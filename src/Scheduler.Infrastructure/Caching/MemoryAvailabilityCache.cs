using Microsoft.Extensions.Caching.Memory;
using Scheduler.Application.Interfaces;

namespace Scheduler.Infrastructure.Caching;

// Cache is a read-performance optimization only, never the authority for booking
// correctness — see architecture.md §5 Cache Strategy. Redis-ready: swapping this for
// a RedisAvailabilityCache implementing the same interface requires no caller changes.
public sealed class MemoryAvailabilityCache : IAvailabilityCache
{
    private readonly IMemoryCache _cache;

    public MemoryAvailabilityCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task InvalidateAsync(Guid technicianId, Guid serviceBayId, CancellationToken cancellationToken = default)
    {
        _cache.Remove(CacheKey(technicianId));
        _cache.Remove(CacheKey(serviceBayId));
        return Task.CompletedTask;
    }

    internal static string CacheKey(Guid resourceId) => $"availability:{resourceId}";
}
