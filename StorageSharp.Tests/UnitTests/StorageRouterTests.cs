using System.Text;
using StorageSharp.Storages;
using Xunit;

namespace StorageSharp.Tests.UnitTests;

public class StorageRouterTests
{
    [Fact]
    public async Task ReadAsync_WithMatchingBranch_ShouldUseCorrectStorage()
    {
        // Given
        var httpStorage = new MemoryStorage();
        var defaultStorage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("http data");
        var key = "http://example.com/file.txt";
        
        await httpStorage.WriteAsync(key, testData);
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("http://") || key.StartsWith("https://"),
                httpStorage)
        }, defaultStorage);

        // When
        var result = await storage.ReadAsync(key);

        // Then
        Assert.Equal(testData, result);
    }

    [Fact]
    public async Task ReadAsync_WithKeyFormatter_ShouldTransformKey()
    {
        // Given
        var fileStorage = new MemoryStorage();
        var defaultStorage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("file data");
        var originalKey = "file://data/test.txt";
        var transformedKey = "data/test.txt";
        
        await fileStorage.WriteAsync(transformedKey, testData);
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("file://"),
                key => key.Substring("file://".Length),
                fileStorage)
        }, defaultStorage);

        // When
        var result = await storage.ReadAsync(originalKey);

        // Then
        Assert.Equal(testData, result);
    }

    [Fact]
    public async Task ReadAsync_WithNoMatchingBranch_ShouldUseDefaultStorage()
    {
        // Given
        var httpStorage = new MemoryStorage();
        var defaultStorage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("default data");
        var key = "regular-file.txt";
        
        await defaultStorage.WriteAsync(key, testData);
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("http://") || key.StartsWith("https://"),
                httpStorage)
        }, defaultStorage);

        // When
        var result = await storage.ReadAsync(key);

        // Then
        Assert.Equal(testData, result);
    }

    [Fact]
    public async Task ReadAsync_WithNoMatchingBranchAndNoDefault_ShouldThrowException()
    {
        // Given
        var httpStorage = new MemoryStorage();
        var key = "regular-file.txt";
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("http://") || key.StartsWith("https://"),
                httpStorage)
        }, null);

        // When & Then
        await Assert.ThrowsAsync<KeyNotFoundException>(() => storage.ReadAsync(key));
    }

    [Fact]
    public async Task WriteAsync_WithMatchingBranch_ShouldWriteToCorrectStorage()
    {
        // Given
        var httpStorage = new MemoryStorage();
        var defaultStorage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("http data");
        var key = "https://example.com/file.txt";
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("http://") || key.StartsWith("https://"),
                httpStorage)
        }, defaultStorage);

        // When
        await storage.WriteAsync(key, testData);

        // Then
        var result = await httpStorage.ReadAsync(key);
        Assert.Equal(testData, result);
        Assert.Equal(0, defaultStorage.Count);
    }

    [Fact]
    public async Task WriteAsync_WithKeyFormatter_ShouldTransformKeyBeforeWrite()
    {
        // Given
        var fileStorage = new MemoryStorage();
        var defaultStorage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("file data");
        var originalKey = "file://data/test.txt";
        var transformedKey = "data/test.txt";
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("file://"),
                key => key.Substring("file://".Length),
                fileStorage)
        }, defaultStorage);

        // When
        await storage.WriteAsync(originalKey, testData);

        // Then
        var result = await fileStorage.ReadAsync(transformedKey);
        Assert.Equal(testData, result);
    }

    [Fact]
    public async Task ListAll_ShouldReturnKeysFromAllStorages()
    {
        // Given
        var httpStorage = new MemoryStorage();
        var fileStorage = new MemoryStorage();
        var defaultStorage = new MemoryStorage();
        
        await httpStorage.WriteAsync("http://example.com/file1.txt", Encoding.UTF8.GetBytes("data1"));
        await fileStorage.WriteAsync("local/file2.txt", Encoding.UTF8.GetBytes("data2"));
        await defaultStorage.WriteAsync("regular-file3.txt", Encoding.UTF8.GetBytes("data3"));
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("http://") || key.StartsWith("https://"),
                httpStorage),
            new StorageRouter.Branch(
                key => key.StartsWith("file://"),
                key => key.Substring("file://".Length),
                fileStorage)
        }, defaultStorage);

        // When
        var keys = await storage.ListAll();

        // Then
        Assert.Contains("http://example.com/file1.txt", keys);
        Assert.Contains("local/file2.txt", keys);
        Assert.Contains("regular-file3.txt", keys);
        Assert.Equal(3, keys.Length);
    }

    [Fact]
    public async Task ListAll_WithDuplicateKeys_ShouldReturnUniqueKeys()
    {
        // Given
        var httpStorage = new MemoryStorage();
        var defaultStorage = new MemoryStorage();
        var duplicateKey = "duplicate.txt";
        
        await httpStorage.WriteAsync(duplicateKey, Encoding.UTF8.GetBytes("data1"));
        await defaultStorage.WriteAsync(duplicateKey, Encoding.UTF8.GetBytes("data2"));
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("http://") || key.StartsWith("https://"),
                httpStorage)
        }, defaultStorage);

        // When
        var keys = await storage.ListAll();

        // Then
        Assert.Single(keys.Where(k => k == duplicateKey));
    }

    [Fact]
    public async Task ReadToStreamAsync_ShouldWorkWithMatchingBranch()
    {
        // Given
        var httpStorage = new MemoryStorage();
        var defaultStorage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("stream data");
        var key = "http://example.com/stream.txt";
        
        await httpStorage.WriteAsync(key, testData);
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("http://") || key.StartsWith("https://"),
                httpStorage)
        }, defaultStorage);

        // When
        using var stream = await storage.ReadToStreamAsync(key);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var result = memoryStream.ToArray();

        // Then
        Assert.Equal(testData, result);
    }

    [Fact]
    public async Task WriteAsync_WithStream_ShouldWorkWithMatchingBranch()
    {
        // Given
        var httpStorage = new MemoryStorage();
        var defaultStorage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("stream data");
        var key = "http://example.com/stream.txt";
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("http://") || key.StartsWith("https://"),
                httpStorage)
        }, defaultStorage);

        // When
        using var stream = new MemoryStream(testData);
        await storage.WriteAsync(key, stream);

        // Then
        var result = await httpStorage.ReadAsync(key);
        Assert.Equal(testData, result);
    }

    [Fact]
    public async Task MultipleBranches_ShouldSelectFirstMatchingBranch()
    {
        // Given
        var httpStorage = new MemoryStorage();
        var genericStorage = new MemoryStorage();
        var defaultStorage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("http data");
        var key = "http://example.com/file.txt";
        
        await httpStorage.WriteAsync(key, testData);
        await genericStorage.WriteAsync(key, Encoding.UTF8.GetBytes("generic data"));
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("http://") || key.StartsWith("https://"),
                httpStorage),
            new StorageRouter.Branch(
                key => key.Contains("example.com"),
                genericStorage)
        }, defaultStorage);

        // When
        var result = await storage.ReadAsync(key);

        // Then
        Assert.Equal(testData, result); // Should use httpStorage (first match)
    }

    [Fact]
    public async Task ListAll_WithFailingStorage_ShouldContinueWithOtherStorages()
    {
        // Given
        var workingStorage = new MemoryStorage();
        var failingStorage = new FailingStorage();
        var defaultStorage = new MemoryStorage();
        
        await workingStorage.WriteAsync("working.txt", Encoding.UTF8.GetBytes("data"));
        await defaultStorage.WriteAsync("default.txt", Encoding.UTF8.GetBytes("data"));
        
        var storage = new StorageRouter(new[]
        {
            new StorageRouter.Branch(
                key => key.StartsWith("working"),
                workingStorage),
            new StorageRouter.Branch(
                key => key.StartsWith("failing"),
                failingStorage)
        }, defaultStorage);

        // When
        var keys = await storage.ListAll();

        // Then
        Assert.Contains("working.txt", keys);
        Assert.Contains("default.txt", keys);
        Assert.Equal(2, keys.Length);
    }

    // Helper class for testing error scenarios
    private class FailingStorage : IStorage
    {
        public Task<string[]> ListAll(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Storage failed");
        }

        public Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Storage failed");
        }

        public Task WriteAsync(string key, byte[] data, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Storage failed");
        }

        public Task<Stream> ReadToStreamAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Storage failed");
        }

        public Task WriteAsync(string key, Stream stream, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Storage failed");
        }
    }
} 