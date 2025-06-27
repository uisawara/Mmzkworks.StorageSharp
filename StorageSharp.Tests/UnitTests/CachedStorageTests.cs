using System.Text;
using StorageSharp.Storages;
using Xunit;

namespace StorageSharp.Tests.UnitTests;

public class CachedStorageTests
{
    [Fact]
    public async Task ReadAsync_WithCacheHit_ShouldReturnFromCache()
    {
        // Given
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var cachedStorage = new CachedStorage(cache, origin);
        var testData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";
        await cachedStorage.WriteAsync(key, testData);

        // When
        var result = await cachedStorage.ReadAsync(key);

        // Then
        Assert.Equal(testData, result);
        Assert.Equal(1, cachedStorage.CacheHitCount);
    }

    [Fact]
    public async Task ReadAsync_WithCacheMiss_ShouldReturnFromOriginAndCache()
    {
        // Given
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var cachedStorage = new CachedStorage(cache, origin);
        var testData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";
        await origin.WriteAsync(key, testData);

        // When
        var result = await cachedStorage.ReadAsync(key);

        // Then
        Assert.Equal(testData, result);
        Assert.Equal(0, cachedStorage.CacheHitCount);

        var cachedData = await cache.ReadAsync(key);
        Assert.Equal(testData, cachedData);
    }

    [Fact]
    public async Task WriteAsync_ShouldWriteToBothCacheAndOrigin()
    {
        // Given
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var cachedStorage = new CachedStorage(cache, origin);
        var testData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";

        // When
        await cachedStorage.WriteAsync(key, testData);

        // Then
        var cacheData = await cache.ReadAsync(key);
        var originData = await origin.ReadAsync(key);

        Assert.Equal(testData, cacheData);
        Assert.Equal(testData, originData);
    }

    [Fact]
    public async Task ListAll_ShouldReturnFromOrigin()
    {
        // Given
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var cachedStorage = new CachedStorage(cache, origin);
        var testData = Encoding.UTF8.GetBytes("test data");
        await origin.WriteAsync("file1.txt", testData);
        await origin.WriteAsync("file2.txt", testData);

        // When
        var keys = await cachedStorage.ListAll();

        // Then
        Assert.Contains("file1.txt", keys);
        Assert.Contains("file2.txt", keys);
        Assert.Equal(2, keys.Length);
    }

    [Fact]
    public async Task ClearCache_ShouldClearCache()
    {
        // Given
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var cachedStorage = new CachedStorage(cache, origin);
        var testData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";
        await cachedStorage.WriteAsync(key, testData);
        Assert.Equal(1, cachedStorage.CacheHitCount);

        // When
        await cachedStorage.ClearCache();

        // Then
        Assert.Equal(0, cachedStorage.CacheHitCount);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public async Task ReadToStreamAsync_WithCacheHit_ShouldReturnFromCache()
    {
        // Given
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var cachedStorage = new CachedStorage(cache, origin);
        var testData = "test stream data";
        var key = "stream.txt";
        await cachedStorage.WriteAsync(key, Encoding.UTF8.GetBytes(testData));

        // When
        using var reader = await cachedStorage.ReadToStreamAsync(key);
        var content = await reader.ReadToEndAsync();

        // Then
        Assert.Equal(testData, content);
        Assert.Equal(1, cachedStorage.CacheHitCount);
    }

    [Fact]
    public async Task WriteAsync_WithStream_ShouldWriteToBothCacheAndOrigin()
    {
        // Given
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var cachedStorage = new CachedStorage(cache, origin);
        var testData = "test stream data";
        var key = "stream.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(testData));
        using var reader = new StreamReader(stream);

        // When
        await cachedStorage.WriteAsync(key, reader);

        // Then
        var cacheData = await cache.ReadAsync(key);
        var originData = await origin.ReadAsync(key);

        var cacheContent = Encoding.UTF8.GetString(cacheData);
        var originContent = Encoding.UTF8.GetString(originData);

        Assert.Equal(testData, cacheContent);
        Assert.Equal(testData, originContent);
    }

    [Fact]
    public async Task CacheHitCount_ShouldTrackCacheHits()
    {
        // Given
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var cachedStorage = new CachedStorage(cache, origin);
        var testData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";
        await cachedStorage.WriteAsync(key, testData);
        Assert.Equal(1, cachedStorage.CacheHitCount);

        // When
        await cachedStorage.ReadAsync(key);

        // Then
        Assert.Equal(1, cachedStorage.CacheHitCount);

        // When
        await cachedStorage.ReadAsync(key);

        // Then
        Assert.Equal(1, cachedStorage.CacheHitCount);
    }
}