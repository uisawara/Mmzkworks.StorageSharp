using System.Text;
using StorageSharp.Storages;
using Xunit;

namespace StorageSharp.Tests.UnitTests;

public class MemoryStorageTests
{
    [Fact]
    public async Task WriteAsync_ShouldStoreData()
    {
        // Given
        var storage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";

        // When
        await storage.WriteAsync(key, testData);

        // Then
        Assert.Equal(1, storage.Count);
    }

    [Fact]
    public async Task ReadAsync_ShouldReturnCorrectData()
    {
        // Given
        var storage = new MemoryStorage();
        var expectedData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";
        await storage.WriteAsync(key, expectedData);

        // When
        var actualData = await storage.ReadAsync(key);

        // Then
        Assert.Equal(expectedData, actualData);
    }

    [Fact]
    public async Task ReadAsync_WithNonExistentKey_ShouldThrowKeyNotFoundException()
    {
        // Given
        var storage = new MemoryStorage();
        var key = "nonexistent.txt";

        // When & Then
        await Assert.ThrowsAsync<KeyNotFoundException>(() => storage.ReadAsync(key));
    }

    [Fact]
    public async Task ListAll_ShouldReturnAllKeys()
    {
        // Given
        var storage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("test data");
        await storage.WriteAsync("file1.txt", testData);
        await storage.WriteAsync("file2.txt", testData);

        // When
        var keys = await storage.ListAll();

        // Then
        Assert.Contains("file1.txt", keys);
        Assert.Contains("file2.txt", keys);
        Assert.Equal(2, keys.Length);
    }

    [Fact]
    public async Task WriteAsync_WithEmptyData_ShouldRemoveKey()
    {
        // Given
        var storage = new MemoryStorage();
        var testData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";
        await storage.WriteAsync(key, testData);
        Assert.Equal(1, storage.Count);

        // When
        await storage.WriteAsync(key, new byte[0]);

        // Then
        Assert.Equal(0, storage.Count);
    }

    [Fact]
    public async Task WriteAsync_WithStream_ShouldStoreData()
    {
        // Given
        var storage = new MemoryStorage();
        var testData = "test stream data";
        var key = "stream.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(testData));
        using var reader = new StreamReader(stream);

        // When
        await storage.WriteAsync(key, reader);

        // Then
        var storedData = await storage.ReadAsync(key);
        var content = Encoding.UTF8.GetString(storedData);
        Assert.Equal(testData, content);
    }

    [Fact]
    public async Task ReadToStreamAsync_ShouldReturnStreamReader()
    {
        // Given
        var storage = new MemoryStorage();
        var testData = "test stream data";
        var key = "stream.txt";
        await storage.WriteAsync(key, Encoding.UTF8.GetBytes(testData));

        // When
        using var reader = await storage.ReadToStreamAsync(key);
        var content = await reader.ReadToEndAsync();

        // Then
        Assert.Equal(testData, content);
    }

    [Fact]
    public void Clear_ShouldRemoveAllData()
    {
        // Given
        var storage = new MemoryStorage();
        storage.WriteAsync("file1.txt", Encoding.UTF8.GetBytes("data1")).Wait();
        storage.WriteAsync("file2.txt", Encoding.UTF8.GetBytes("data2")).Wait();
        Assert.Equal(2, storage.Count);

        // When
        storage.Clear();

        // Then
        Assert.Equal(0, storage.Count);
    }

    [Fact]
    public void Count_ShouldReturnCorrectNumberOfItems()
    {
        // Given
        var storage = new MemoryStorage();
        Assert.Equal(0, storage.Count);

        // When
        storage.WriteAsync("file1.txt", Encoding.UTF8.GetBytes("data1")).Wait();

        // Then
        Assert.Equal(1, storage.Count);

        // When
        storage.WriteAsync("file2.txt", Encoding.UTF8.GetBytes("data2")).Wait();

        // Then
        Assert.Equal(2, storage.Count);
    }
}