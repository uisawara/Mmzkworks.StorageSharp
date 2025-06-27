using System.Text;
using StorageSharp.Storages;
using Xunit;

namespace StorageSharp.Tests.UnitTests;

public class FileStorageTests : IDisposable
{
    private readonly string _testDirectory;

    public FileStorageTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "StorageSharpTests", Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public async Task WriteAsync_ShouldCreateFile()
    {
        // Given
        var storage = new FileStorage(_testDirectory);
        var testData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";

        // When
        await storage.WriteAsync(key, testData);

        // Then
        var filePath = Path.Combine(_testDirectory, key);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task ReadAsync_ShouldReturnCorrectData()
    {
        // Given
        var storage = new FileStorage(_testDirectory);
        var expectedData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";
        await storage.WriteAsync(key, expectedData);

        // When
        var actualData = await storage.ReadAsync(key);

        // Then
        Assert.Equal(expectedData, actualData);
    }

    [Fact]
    public async Task ReadAsync_WithNonExistentKey_ShouldThrowFileNotFoundException()
    {
        // Given
        var storage = new FileStorage(_testDirectory);
        var key = "nonexistent.txt";

        // When & Then
        await Assert.ThrowsAsync<FileNotFoundException>(() => storage.ReadAsync(key));
    }

    [Fact]
    public async Task ListAll_ShouldReturnAllKeys()
    {
        // Given
        var storage = new FileStorage(_testDirectory);
        var testData = Encoding.UTF8.GetBytes("test data");
        await storage.WriteAsync("file1.txt", testData);
        await storage.WriteAsync("file2.txt", testData);
        await storage.WriteAsync("subdir/file3.txt", testData);

        // When
        var keys = await storage.ListAll();

        // Then
        Assert.Contains("file1.txt", keys);
        Assert.Contains("file2.txt", keys);
        Assert.Contains("subdir/file3.txt", keys);
        Assert.Equal(3, keys.Length);
    }

    [Fact]
    public async Task WriteAsync_WithEmptyData_ShouldDeleteFile()
    {
        // Given
        var storage = new FileStorage(_testDirectory);
        var testData = Encoding.UTF8.GetBytes("test data");
        var key = "test.txt";
        await storage.WriteAsync(key, testData);
        Assert.True(File.Exists(Path.Combine(_testDirectory, key)));

        // When
        await storage.WriteAsync(key, new byte[0]);

        // Then
        Assert.False(File.Exists(Path.Combine(_testDirectory, key)));
    }

    [Fact]
    public async Task WriteAsync_WithStream_ShouldCreateFile()
    {
        // Given
        var storage = new FileStorage(_testDirectory);
        var testData = "test stream data";
        var key = "stream.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(testData));
        using var reader = new StreamReader(stream);

        // When
        await storage.WriteAsync(key, reader);

        // Then
        var filePath = Path.Combine(_testDirectory, key);
        Assert.True(File.Exists(filePath));
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Equal(testData, content);
    }

    [Fact]
    public async Task ReadToStreamAsync_ShouldReturnStreamReader()
    {
        // Given
        var storage = new FileStorage(_testDirectory);
        var testData = "test stream data";
        var key = "stream.txt";
        await storage.WriteAsync(key, Encoding.UTF8.GetBytes(testData));

        // When
        using var reader = await storage.ReadToStreamAsync(key);
        var content = await reader.ReadToEndAsync();

        // Then
        Assert.Equal(testData, content);
    }
}