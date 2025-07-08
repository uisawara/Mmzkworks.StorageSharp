using System.Text;
using StorageSharp.Packs;
using StorageSharp.Storages;
using Xunit;

namespace StorageSharp.Tests.IntegrationTests;

public class StorageIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _testDirectory;

    public StorageIntegrationTests()
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
    public async Task FileStorage_CompleteWorkflow()
    {
        var storage = new FileStorage(_testDirectory);
        var testData = Encoding.UTF8.GetBytes("integration test data");
        var key = "integration_test.txt";

        await storage.WriteAsync(key, testData);
        var readData = await storage.ReadAsync(key);
        var keys = await storage.ListAll();

        Assert.Equal(testData, readData);
        Assert.Contains(key, keys);
    }

    [Fact]
    public async Task MemoryStorage_CompleteWorkflow()
    {
        var storage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("integration test data");
        var key = "integration_test.txt";

        await storage.WriteAsync(key, testData);
        var readData = await storage.ReadAsync(key);
        var keys = await storage.ListAll();

        Assert.Equal(testData, readData);
        Assert.Contains(key, keys);
    }

    [Fact]
    public async Task CachedStorage_CompleteWorkflow()
    {
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var storage = new CachedStorage(cache, origin);
        var testData = Encoding.UTF8.GetBytes("integration test data");
        var key = "integration_test.txt";

        await storage.WriteAsync(key, testData);
        var readData = await storage.ReadAsync(key);
        var keys = await storage.ListAll();

        Assert.Equal(testData, readData);
        Assert.Contains(key, keys);
    }

    [Fact]
    public async Task ZippedPackages_CompleteWorkflow()
    {
        var storage = new MemoryStorage();
        var settings = new ZippedPacks.Settings(_tempDirectory);
        var packages = new ZippedPacks(settings, storage);

        var testDir = Path.Combine(_testDirectory, "test_package");
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "test.txt"), "test data");

        var scheme = await packages.Add(testDir);
        var schemes = await packages.ListAll();
        Assert.Contains(schemes, s => s.DirectoryPath == testDir);

        var loadedPath = await packages.Load(scheme);
        Assert.True(Directory.Exists(loadedPath));
        Assert.True(File.Exists(Path.Combine(loadedPath, "test.txt")));

        await packages.Unload(scheme);
        Assert.False(Directory.Exists(loadedPath));

        await packages.Delete(scheme);
        var finalSchemes = await packages.ListAll();
        Assert.Empty(finalSchemes);
    }

    [Fact]
    public async Task MultipleStorageTypes_Integration()
    {
        var fileStorage = new FileStorage(_testDirectory);
        var memoryStorage = new MemoryStorage();
        var cache = new MemoryStorage();
        var cachedStorage = new CachedStorage(cache, memoryStorage);

        var testData = Encoding.UTF8.GetBytes("multi-storage test data");
        var key = "multi_test.txt";

        await fileStorage.WriteAsync(key, testData);
        await memoryStorage.WriteAsync(key, testData);
        await cachedStorage.WriteAsync(key, testData);

        var fileData = await fileStorage.ReadAsync(key);
        var memoryData = await memoryStorage.ReadAsync(key);
        var cachedData = await cachedStorage.ReadAsync(key);

        Assert.Equal(testData, fileData);
        Assert.Equal(testData, memoryData);
        Assert.Equal(testData, cachedData);
    }

    [Fact]
    public async Task StorageAndPackages_Integration()
    {
        var storage = new MemoryStorage();
        var settings = new ZippedPacks.Settings(_tempDirectory);
        var packages = new ZippedPacks(settings, storage);

        var testDir = Path.Combine(_testDirectory, "test_package");
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "test.txt"), "test data");

        var scheme = await packages.Add(testDir);
        var loadedPath = await packages.Load(scheme);

        var fileContent = File.ReadAllText(Path.Combine(loadedPath, "test.txt"));
        Assert.Equal("test data", fileContent);

        await packages.Unload(scheme);
    }

    [Fact]
    public async Task LargeData_Handling()
    {
        var storage = new FileStorage(_testDirectory);
        var largeData = new byte[1024 * 1024];
        new Random().NextBytes(largeData);
        var key = "large_file.bin";

        await storage.WriteAsync(key, largeData);
        var readData = await storage.ReadAsync(key);

        Assert.Equal(largeData, readData);
    }

    [Fact]
    public async Task ConcurrentAccess_FileStorage()
    {
        var storage = new FileStorage(_testDirectory);
        var tasks = new List<Task>();

        for (var i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var data = Encoding.UTF8.GetBytes($"concurrent test {index}");
                var key = $"concurrent_{index}.txt";
                await storage.WriteAsync(key, data);
                var readData = await storage.ReadAsync(key);
                Assert.Equal(data, readData);
            }));
        }

        await Task.WhenAll(tasks);
        var keys = await storage.ListAll();
        Assert.Equal(10, keys.Length);
    }

    [Fact]
    public async Task StreamOperations_Integration()
    {
        var storage = new FileStorage(_testDirectory);
        var testData = Encoding.UTF8.GetBytes("stream integration test data");
        var key = "stream_integration.txt";

        using (var stream = new MemoryStream(testData))
        {
            await storage.WriteAsync(key, stream);
        }

        using (var stream = await storage.ReadToStreamAsync(key))
        using (var memoryStream = new MemoryStream())
        {
            await stream.CopyToAsync(memoryStream);
            var readData = memoryStream.ToArray();
            Assert.Equal(testData, readData);
        }
    }

    [Fact]
    public async Task FileDeletion_Integration()
    {
        var storage = new FileStorage(_testDirectory);
        var testData = Encoding.UTF8.GetBytes("deletion test");
        var key = "delete_test.txt";

        await storage.WriteAsync(key, testData);
        var keys = await storage.ListAll();
        Assert.Contains(key, keys);

        await storage.WriteAsync(key, new byte[0]);
        keys = await storage.ListAll();
        Assert.DoesNotContain(key, keys);
    }

    [Fact]
    public async Task NestedDirectories_Integration()
    {
        var storage = new FileStorage(_testDirectory);
        var testData = Encoding.UTF8.GetBytes("nested directory test");
        var key = "dir1/dir2/nested_test.txt";

        await storage.WriteAsync(key, testData);
        var readData = await storage.ReadAsync(key);
        var keys = await storage.ListAll();

        Assert.Equal(testData, readData);
        Assert.Contains(key, keys);
    }
}