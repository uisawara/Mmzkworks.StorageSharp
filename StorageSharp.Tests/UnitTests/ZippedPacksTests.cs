using StorageSharp.Packs;
using StorageSharp.Storages;
using Xunit;

namespace StorageSharp.Tests.UnitTests;

public class ZippedPacksTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _testDirectory;

    public ZippedPacksTests()
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
    public async Task Add_ShouldCreateZipFile()
    {
        // Given
        var storage = new MemoryStorage();
        var settings = new ZippedPacks.Settings(_tempDirectory);
        var packages = new ZippedPacks(settings, storage);
        var testDir = Path.Combine(_testDirectory, "test_package");
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "test.txt"), "test data");

        // When
        var scheme = await packages.Add(testDir);

        // Then
        Assert.NotNull(scheme);
        Assert.Equal(testDir, scheme.DirectoryPath);
    }

    [Fact]
    public async Task Load_ShouldExtractFiles()
    {
        // Given
        var storage = new MemoryStorage();
        var settings = new ZippedPacks.Settings(_tempDirectory);
        var packages = new ZippedPacks(settings, storage);
        var testDir = Path.Combine(_testDirectory, "test_package");
        Directory.CreateDirectory(testDir);
        var testFile = Path.Combine(testDir, "test.txt");
        File.WriteAllText(testFile, "test data");
        var scheme = await packages.Add(testDir);

        // When
        var loadedPath = await packages.Load(scheme);

        // Then
        Assert.True(Directory.Exists(loadedPath));
        Assert.True(File.Exists(Path.Combine(loadedPath, "test.txt")));
    }

    [Fact]
    public async Task ListAll_ShouldReturnSchemes()
    {
        // Given
        var storage = new MemoryStorage();
        var settings = new ZippedPacks.Settings(_tempDirectory);
        var packages = new ZippedPacks(settings, storage);
        var testDir1 = Path.Combine(_testDirectory, "test_package1");
        var testDir2 = Path.Combine(_testDirectory, "test_package2");
        Directory.CreateDirectory(testDir1);
        Directory.CreateDirectory(testDir2);
        File.WriteAllText(Path.Combine(testDir1, "test1.txt"), "test data 1");
        File.WriteAllText(Path.Combine(testDir2, "test2.txt"), "test data 2");
        await packages.Add(testDir1);
        await packages.Add(testDir2);

        // When
        var schemes = await packages.ListAll();

        // Then
        Assert.Equal(2, schemes.Length);
        Assert.Contains(schemes, s => s.DirectoryPath == testDir1);
        Assert.Contains(schemes, s => s.DirectoryPath == testDir2);
    }

    [Fact]
    public async Task Delete_ShouldRemovePackage()
    {
        // Given
        var storage = new MemoryStorage();
        var settings = new ZippedPacks.Settings(_tempDirectory);
        var packages = new ZippedPacks(settings, storage);
        var testDir = Path.Combine(_testDirectory, "test_package");
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "test.txt"), "test data");
        var scheme = await packages.Add(testDir);
        var initialSchemes = await packages.ListAll();
        Assert.Single(initialSchemes);

        // When
        await packages.Delete(scheme);

        // Then
        var finalSchemes = await packages.ListAll();
        Assert.Empty(finalSchemes);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllPackages()
    {
        // Given
        var storage = new MemoryStorage();
        var settings = new ZippedPacks.Settings(_tempDirectory);
        var packages = new ZippedPacks(settings, storage);
        var testDir1 = Path.Combine(_testDirectory, "test_package1");
        var testDir2 = Path.Combine(_testDirectory, "test_package2");
        Directory.CreateDirectory(testDir1);
        Directory.CreateDirectory(testDir2);
        File.WriteAllText(Path.Combine(testDir1, "test1.txt"), "test data 1");
        File.WriteAllText(Path.Combine(testDir2, "test2.txt"), "test data 2");
        await packages.Add(testDir1);
        await packages.Add(testDir2);
        var initialSchemes = await packages.ListAll();
        Assert.Equal(2, initialSchemes.Length);

        // When
        await packages.Clear();

        // Then
        var finalSchemes = await packages.ListAll();
        Assert.Empty(finalSchemes);
    }

    [Fact]
    public async Task Unload_ShouldRemoveFromLoadedPackages()
    {
        // Given
        var storage = new MemoryStorage();
        var settings = new ZippedPacks.Settings(_tempDirectory);
        var packages = new ZippedPacks(settings, storage);
        var testDir = Path.Combine(_testDirectory, "test_package");
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "test.txt"), "test data");
        var scheme = await packages.Add(testDir);
        var loadedPath = await packages.Load(scheme);
        Assert.True(Directory.Exists(loadedPath));

        // When
        await packages.Unload(scheme);

        // Then
        Assert.False(Directory.Exists(loadedPath));
    }

    [Fact]
    public async Task Load_ShouldReturnSamePathForSameScheme()
    {
        // Given
        var storage = new MemoryStorage();
        var settings = new ZippedPacks.Settings(_tempDirectory);
        var packages = new ZippedPacks(settings, storage);
        var testDir = Path.Combine(_testDirectory, "test_package");
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "test.txt"), "test data");
        var scheme = await packages.Add(testDir);

        // When
        var path1 = await packages.Load(scheme);
        var path2 = await packages.Load(scheme);

        // Then
        Assert.Equal(path1, path2);
    }
}