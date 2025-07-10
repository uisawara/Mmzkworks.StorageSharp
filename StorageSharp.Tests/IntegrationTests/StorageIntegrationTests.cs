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
    public async Task EncryptedStorage_CompleteWorkflow()
    {
        var baseStorage = new MemoryStorage();
        var password = "integration-test-password";
        var storage = new EncryptedStorage(baseStorage, password);
        var testData = Encoding.UTF8.GetBytes("encrypted integration test data");
        var key = "encrypted_integration_test.txt";

        await storage.WriteAsync(key, testData);
        var readData = await storage.ReadAsync(key);
        var keys = await storage.ListAll();

        Assert.Equal(testData, readData);
        Assert.Contains(key, keys);

        // Verify that the underlying storage contains encrypted data
        var encryptedData = await baseStorage.ReadAsync(key);
        Assert.NotEqual(testData, encryptedData);
    }

    [Fact]
    public async Task EncryptedStorage_WithDifferentPasswords()
    {
        var baseStorage = new MemoryStorage();
        var storage1 = new EncryptedStorage(baseStorage, "password1");
        var storage2 = new EncryptedStorage(baseStorage, "password2");
        var testData = Encoding.UTF8.GetBytes("test data for different passwords");
        var key = "different_passwords_test.txt";

        // Write with first password
        await storage1.WriteAsync(key, testData);

        // Reading with same password should work
        var readData1 = await storage1.ReadAsync(key);
        Assert.Equal(testData, readData1);

        // Reading with different password should fail
        await Assert.ThrowsAsync<System.Security.Cryptography.CryptographicException>(
            () => storage2.ReadAsync(key));
    }

    [Fact]
    public async Task EncryptedStorage_WithDifferentStorageBackends()
    {
        var testData = Encoding.UTF8.GetBytes("backend integration test data");
        var password = "backend-test-password";
        var key = "backend_test.txt";

        // Test with MemoryStorage backend
        var memoryBackend = new MemoryStorage();
        var encryptedMemory = new EncryptedStorage(memoryBackend, password);
        await encryptedMemory.WriteAsync(key, testData);
        var memoryResult = await encryptedMemory.ReadAsync(key);
        Assert.Equal(testData, memoryResult);

        // Test with FileStorage backend
        var fileBackend = new FileStorage(_testDirectory);
        var encryptedFile = new EncryptedStorage(fileBackend, password);
        await encryptedFile.WriteAsync(key, testData);
        var fileResult = await encryptedFile.ReadAsync(key);
        Assert.Equal(testData, fileResult);

        // Test with CachedStorage backend
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var cachedBackend = new CachedStorage(cache, origin);
        var encryptedCached = new EncryptedStorage(cachedBackend, password);
        await encryptedCached.WriteAsync(key, testData);
        var cachedResult = await encryptedCached.ReadAsync(key);
        Assert.Equal(testData, cachedResult);
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

    [Fact]
    public async Task EncryptedStorage_LargeData_Integration()
    {
        var baseStorage = new MemoryStorage();
        var storage = new EncryptedStorage(baseStorage, "large-data-test-password");
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeData);
        var key = "large_encrypted_file.bin";

        await storage.WriteAsync(key, largeData);
        var readData = await storage.ReadAsync(key);

        Assert.Equal(largeData, readData);

        // Verify that the underlying storage contains encrypted data
        var encryptedData = await baseStorage.ReadAsync(key);
        Assert.NotEqual(largeData, encryptedData);
        Assert.True(encryptedData.Length > largeData.Length); // Encrypted data should be larger due to padding
    }

    [Fact]
    public async Task EncryptedStorage_ConcurrentAccess()
    {
        var baseStorage = new MemoryStorage();
        var storage = new EncryptedStorage(baseStorage, "concurrent-test-password");
        var tasks = new List<Task>();

        for (var i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var data = Encoding.UTF8.GetBytes($"concurrent encrypted test {index}");
                var key = $"concurrent_encrypted_{index}.txt";
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
    public async Task EncryptedStorage_StreamOperations_Integration()
    {
        var baseStorage = new FileStorage(_testDirectory);
        var storage = new EncryptedStorage(baseStorage, "stream-test-password");
        var testData = Encoding.UTF8.GetBytes("encrypted stream integration test data");
        var key = "encrypted_stream_integration.txt";

        // Write using stream
        using (var stream = new MemoryStream(testData))
        {
            await storage.WriteAsync(key, stream);
        }

        // Read using stream
        using (var stream = await storage.ReadToStreamAsync(key))
        using (var memoryStream = new MemoryStream())
        {
            await stream.CopyToAsync(memoryStream);
            var readData = memoryStream.ToArray();
            Assert.Equal(testData, readData);
        }

        // Verify the underlying storage contains encrypted data
        var encryptedData = await baseStorage.ReadAsync(key);
        Assert.NotEqual(testData, encryptedData);
    }

    [Fact]
    public async Task EncryptedStorage_EmptyAndNullData_Integration()
    {
        var baseStorage = new MemoryStorage();
        var storage = new EncryptedStorage(baseStorage, "empty-data-test-password");
        var key = "empty_test.txt";

        // Test with empty data
        await storage.WriteAsync(key, new byte[0]);
        var keys = await storage.ListAll();
        Assert.DoesNotContain(key, keys);

        // Test with null data
        await storage.WriteAsync(key, (byte[])null);
        keys = await storage.ListAll();
        Assert.DoesNotContain(key, keys);
    }

    [Fact]
    public async Task EncryptedStorage_CustomKeyIV_Integration()
    {
        var baseStorage = new MemoryStorage();
        var key = new byte[32]; // 32-byte key for AES-256
        var iv = new byte[16];  // 16-byte IV
        
        // Initialize with specific values
        for (var i = 0; i < 32; i++) key[i] = (byte)(i + 1);
        for (var i = 0; i < 16; i++) iv[i] = (byte)(i + 100);

        var storage = new EncryptedStorage(baseStorage, key, iv);
        var testData = Encoding.UTF8.GetBytes("custom key/IV integration test");
        var dataKey = "custom_key_iv_test.txt";

        await storage.WriteAsync(dataKey, testData);
        var readData = await storage.ReadAsync(dataKey);

        Assert.Equal(testData, readData);

        // Verify same key/IV produces same encryption
        var storage2 = new EncryptedStorage(baseStorage, key, iv);
        var readData2 = await storage2.ReadAsync(dataKey);
        Assert.Equal(testData, readData2);
    }

    [Fact]
    public async Task EncryptedStorage_MultipleStorageTypes_Integration()
    {
        var fileStorage = new FileStorage(_testDirectory);
        var memoryStorage1 = new MemoryStorage();
        var memoryStorage2 = new MemoryStorage();
        var cache = new MemoryStorage();
        var origin = new MemoryStorage();
        var cachedStorage = new CachedStorage(cache, origin);

        var encryptedFileStorage = new EncryptedStorage(fileStorage, "file-password");
        var encryptedMemoryStorage = new EncryptedStorage(memoryStorage1, "memory-password");
        var encryptedCachedStorage = new EncryptedStorage(cachedStorage, "cached-password");

        var testData = Encoding.UTF8.GetBytes("multi-encrypted-storage test data");
        var fileKey = "file_encrypted_test.txt";
        var memoryKey = "memory_encrypted_test.txt";
        var cachedKey = "cached_encrypted_test.txt";

        // Write to all encrypted storages with different keys
        await encryptedFileStorage.WriteAsync(fileKey, testData);
        await encryptedMemoryStorage.WriteAsync(memoryKey, testData);
        await encryptedCachedStorage.WriteAsync(cachedKey, testData);

        // Read from all encrypted storages
        var fileData = await encryptedFileStorage.ReadAsync(fileKey);
        var memoryData = await encryptedMemoryStorage.ReadAsync(memoryKey);
        var cachedData = await encryptedCachedStorage.ReadAsync(cachedKey);

        Assert.Equal(testData, fileData);
        Assert.Equal(testData, memoryData);
        Assert.Equal(testData, cachedData);

        // Verify that underlying storages contain encrypted data
        var fileEncrypted = await fileStorage.ReadAsync(fileKey);
        var memoryEncrypted = await memoryStorage1.ReadAsync(memoryKey);
        var cachedEncrypted = await cachedStorage.ReadAsync(cachedKey);

        Assert.NotEqual(testData, fileEncrypted);
        Assert.NotEqual(testData, memoryEncrypted);
        Assert.NotEqual(testData, cachedEncrypted);

        // Different passwords should produce different encrypted data
        Assert.NotEqual(fileEncrypted, memoryEncrypted);
        Assert.NotEqual(memoryEncrypted, cachedEncrypted);
    }
}