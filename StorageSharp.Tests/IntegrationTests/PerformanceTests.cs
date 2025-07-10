using System.Diagnostics;
using System.Text;
using StorageSharp.Packs;
using StorageSharp.Storages;
using Xunit;

namespace StorageSharp.Tests.IntegrationTests;

public class PerformanceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _testDirectory;

    public PerformanceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "StorageSharpTests", Guid.NewGuid().ToString());
        _tempDirectory = Path.Combine(Path.GetTempPath(), "StorageSharpTests", Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task FileStorage_PerformanceTest()
    {
        var storage = new FileStorage(_testDirectory);
        var testData = Encoding.UTF8.GetBytes("performance test data");
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        for (var i = 0; i < 100; i++)
        {
            var key = $"perf_test_{i}.txt";
            await storage.WriteAsync(key, testData);
        }

        stopwatch.Stop();

        var writeTime = stopwatch.ElapsedMilliseconds;
        Assert.True(writeTime < 5000, $"Write operation took too long: {writeTime}ms");

        stopwatch.Restart();
        for (var i = 0; i < 100; i++)
        {
            var key = $"perf_test_{i}.txt";
            await storage.ReadAsync(key);
        }

        stopwatch.Stop();

        var readTime = stopwatch.ElapsedMilliseconds;
        Assert.True(readTime < 5000, $"Read operation took too long: {readTime}ms");
    }

    [Fact]
    public async Task MemoryStorage_PerformanceTest()
    {
        var storage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("performance test data");
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        for (var i = 0; i < 1000; i++)
        {
            var key = $"perf_test_{i}.txt";
            await storage.WriteAsync(key, testData);
        }

        stopwatch.Stop();

        var writeTime = stopwatch.ElapsedMilliseconds;
        Assert.True(writeTime < 1000, $"Write operation took too long: {writeTime}ms");

        stopwatch.Restart();
        for (var i = 0; i < 1000; i++)
        {
            var key = $"perf_test_{i}.txt";
            await storage.ReadAsync(key);
        }

        stopwatch.Stop();

        var readTime = stopwatch.ElapsedMilliseconds;
        Assert.True(readTime < 1000, $"Read operation took too long: {readTime}ms");
    }

    [Fact]
    public async Task CachedStorage_PerformanceTest()
    {
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var storage = new CachedStorage(cache, origin);
        var testData = Encoding.UTF8.GetBytes("performance test data");
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        for (var i = 0; i < 500; i++)
        {
            var key = $"perf_test_{i}.txt";
            await storage.WriteAsync(key, testData);
        }

        stopwatch.Stop();

        var writeTime = stopwatch.ElapsedMilliseconds;
        Assert.True(writeTime < 2000, $"Write operation took too long: {writeTime}ms");

        stopwatch.Restart();
        for (var i = 0; i < 500; i++)
        {
            var key = $"perf_test_{i}.txt";
            await storage.ReadAsync(key);
        }

        stopwatch.Stop();

        var readTime = stopwatch.ElapsedMilliseconds;
        Assert.True(readTime < 1000, $"Read operation took too long: {readTime}ms");
    }

    [Fact]
    public async Task EncryptedStorage_PerformanceTest()
    {
        var baseStorage = new MemoryStorage();
        var storage = new EncryptedStorage(baseStorage, "performance-test-password");
        var testData = Encoding.UTF8.GetBytes("encrypted performance test data");
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        for (var i = 0; i < 200; i++)
        {
            var key = $"encrypted_perf_test_{i}.txt";
            await storage.WriteAsync(key, testData);
        }

        stopwatch.Stop();

        var writeTime = stopwatch.ElapsedMilliseconds;
        Assert.True(writeTime < 5000, $"Encrypted write operation took too long: {writeTime}ms");

        stopwatch.Restart();
        for (var i = 0; i < 200; i++)
        {
            var key = $"encrypted_perf_test_{i}.txt";
            await storage.ReadAsync(key);
        }

        stopwatch.Stop();

        var readTime = stopwatch.ElapsedMilliseconds;
        Assert.True(readTime < 5000, $"Encrypted read operation took too long: {readTime}ms");
    }

    [Fact]
    public async Task ZippedPackages_PerformanceTest()
    {
        var storage = new MemoryStorage();
        var settings = new ZippedPacks.Settings(_tempDirectory);
        var packages = new ZippedPacks(settings, storage);
        var stopwatch = new Stopwatch();

        var testDir = Path.Combine(_testDirectory, "perf_package");
        Directory.CreateDirectory(testDir);
        for (var i = 0; i < 10; i++) File.WriteAllText(Path.Combine(testDir, $"file_{i}.txt"), $"test data {i}");

        stopwatch.Start();
        var scheme = await packages.Add(testDir);
        stopwatch.Stop();

        var addTime = stopwatch.ElapsedMilliseconds;
        Assert.True(addTime < 5000, $"Add operation took too long: {addTime}ms");

        stopwatch.Restart();
        var loadedPath = await packages.Load(scheme);
        stopwatch.Stop();

        var loadTime = stopwatch.ElapsedMilliseconds;
        Assert.True(loadTime < 3000, $"Load operation took too long: {loadTime}ms");

        Assert.True(Directory.Exists(loadedPath));
        for (var i = 0; i < 10; i++) Assert.True(File.Exists(Path.Combine(loadedPath, $"file_{i}.txt")));

        await packages.Unload(scheme);
    }

    [Fact]
    public async Task LargeData_PerformanceTest()
    {
        var storage = new MemoryStorage();
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeData);
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        for (var i = 0; i < 10; i++)
        {
            var key = $"large_test_{i}.bin";
            await storage.WriteAsync(key, largeData);
        }

        stopwatch.Stop();

        var writeTime = stopwatch.ElapsedMilliseconds;
        Assert.True(writeTime < 10000, $"Large data write took too long: {writeTime}ms");

        stopwatch.Restart();
        for (var i = 0; i < 10; i++)
        {
            var key = $"large_test_{i}.bin";
            var readData = await storage.ReadAsync(key);
            Assert.Equal(largeData.Length, readData.Length);
        }

        stopwatch.Stop();

        var readTime = stopwatch.ElapsedMilliseconds;
        Assert.True(readTime < 10000, $"Large data read took too long: {readTime}ms");
    }

    [Fact]
    public async Task ConcurrentOperations_PerformanceTest()
    {
        var storage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("concurrent test data");
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        var tasks = new Task[100];
        for (var i = 0; i < 100; i++)
        {
            var key = $"concurrent_test_{i}.txt";
            tasks[i] = storage.WriteAsync(key, testData);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var concurrentWriteTime = stopwatch.ElapsedMilliseconds;
        Assert.True(concurrentWriteTime < 5000, $"Concurrent write took too long: {concurrentWriteTime}ms");

        stopwatch.Restart();
        var readTasks = new Task[100];
        for (var i = 0; i < 100; i++)
        {
            var key = $"concurrent_test_{i}.txt";
            readTasks[i] = storage.ReadAsync(key);
        }

        await Task.WhenAll(readTasks);
        stopwatch.Stop();

        var concurrentReadTime = stopwatch.ElapsedMilliseconds;
        Assert.True(concurrentReadTime < 3000, $"Concurrent read took too long: {concurrentReadTime}ms");
    }

    [Fact]
    public async Task EncryptedStorage_LargeData_PerformanceTest()
    {
        var baseStorage = new MemoryStorage();
        var storage = new EncryptedStorage(baseStorage, "large-data-performance-password");
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeData);
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        for (var i = 0; i < 5; i++)
        {
            var key = $"encrypted_large_test_{i}.bin";
            await storage.WriteAsync(key, largeData);
        }

        stopwatch.Stop();

        var writeTime = stopwatch.ElapsedMilliseconds;
        Assert.True(writeTime < 15000, $"Encrypted large data write took too long: {writeTime}ms");

        stopwatch.Restart();
        for (var i = 0; i < 5; i++)
        {
            var key = $"encrypted_large_test_{i}.bin";
            var readData = await storage.ReadAsync(key);
            Assert.Equal(largeData.Length, readData.Length);
        }

        stopwatch.Stop();

        var readTime = stopwatch.ElapsedMilliseconds;
        Assert.True(readTime < 15000, $"Encrypted large data read took too long: {readTime}ms");
    }

    [Fact]
    public async Task EncryptedStorage_ConcurrentOperations_PerformanceTest()
    {
        var baseStorage = new MemoryStorage();
        var storage = new EncryptedStorage(baseStorage, "concurrent-performance-password");
        var testData = Encoding.UTF8.GetBytes("concurrent encrypted test data");
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        var tasks = new Task[50]; // Reduced from 100 due to encryption overhead
        for (var i = 0; i < 50; i++)
        {
            var key = $"encrypted_concurrent_test_{i}.txt";
            tasks[i] = storage.WriteAsync(key, testData);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var concurrentWriteTime = stopwatch.ElapsedMilliseconds;
        Assert.True(concurrentWriteTime < 10000, $"Encrypted concurrent write took too long: {concurrentWriteTime}ms");

        stopwatch.Restart();
        var readTasks = new Task[50];
        for (var i = 0; i < 50; i++)
        {
            var key = $"encrypted_concurrent_test_{i}.txt";
            readTasks[i] = storage.ReadAsync(key);
        }

        await Task.WhenAll(readTasks);
        stopwatch.Stop();

        var concurrentReadTime = stopwatch.ElapsedMilliseconds;
        Assert.True(concurrentReadTime < 10000, $"Encrypted concurrent read took too long: {concurrentReadTime}ms");
    }

    [Fact]
    public async Task EncryptedStorage_StreamOperations_PerformanceTest()
    {
        var baseStorage = new MemoryStorage();
        var storage = new EncryptedStorage(baseStorage, "stream-performance-password");
        var testData = new byte[1024 * 100]; // 100KB
        new Random().NextBytes(testData);
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        for (var i = 0; i < 20; i++)
        {
            var key = $"encrypted_stream_perf_{i}.bin";
            using var stream = new MemoryStream(testData);
            await storage.WriteAsync(key, stream);
        }

        stopwatch.Stop();

        var streamWriteTime = stopwatch.ElapsedMilliseconds;
        Assert.True(streamWriteTime < 8000, $"Encrypted stream write took too long: {streamWriteTime}ms");

        stopwatch.Restart();
        for (var i = 0; i < 20; i++)
        {
            var key = $"encrypted_stream_perf_{i}.bin";
            using var readStream = await storage.ReadToStreamAsync(key);
            using var memoryStream = new MemoryStream();
            await readStream.CopyToAsync(memoryStream);
            var readData = memoryStream.ToArray();
            Assert.Equal(testData.Length, readData.Length);
        }

        stopwatch.Stop();

        var streamReadTime = stopwatch.ElapsedMilliseconds;
        Assert.True(streamReadTime < 8000, $"Encrypted stream read took too long: {streamReadTime}ms");
    }

    [Fact]
    public async Task EncryptedStorage_EncryptionOverhead_PerformanceTest()
    {
        var baseStorage = new MemoryStorage();
        var plaintextStorage = new MemoryStorage();
        var encryptedStorage = new EncryptedStorage(baseStorage, "overhead-test-password");
        var testData = Encoding.UTF8.GetBytes("encryption overhead test data");
        var iterations = 500;

        var plaintextStopwatch = new Stopwatch();
        var encryptedStopwatch = new Stopwatch();

        // Measure plaintext operations
        plaintextStopwatch.Start();
        for (var i = 0; i < iterations; i++)
        {
            var key = $"plaintext_test_{i}.txt";
            await plaintextStorage.WriteAsync(key, testData);
            await plaintextStorage.ReadAsync(key);
        }
        plaintextStopwatch.Stop();

        // Measure encrypted operations
        encryptedStopwatch.Start();
        for (var i = 0; i < iterations; i++)
        {
            var key = $"encrypted_test_{i}.txt";
            await encryptedStorage.WriteAsync(key, testData);
            await encryptedStorage.ReadAsync(key);
        }
        encryptedStopwatch.Stop();

        var plaintextTime = plaintextStopwatch.ElapsedMilliseconds;
        var encryptedTime = encryptedStopwatch.ElapsedMilliseconds;
        
        // Avoid division by zero for very fast operations
        if (plaintextTime == 0) plaintextTime = 1;
        
        var overhead = (double)encryptedTime / plaintextTime;

        // Encryption should not add more than 50x overhead for small operations
        Assert.True(overhead < 50.0, $"Encryption overhead too high: {overhead:F2}x (plaintext: {plaintextTime}ms, encrypted: {encryptedTime}ms)");
    }
}