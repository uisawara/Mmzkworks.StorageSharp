using System.Text;
using StorageSharp.Packs;
using StorageSharp.Storages;

namespace StorageSharpExample;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("StorageSharp Sample Program");
        Console.WriteLine("================================");

        // Basic storage usage example
        await BasicStorageExample();

        Console.WriteLine();

        // Cached storage usage example
        await CachedStorageExample();

        Console.WriteLine();

        // ZIP packages usage example
        await ZippedPackagesExample();

        Console.WriteLine("All samples completed.");
    }

    private static async Task BasicStorageExample()
    {
        Console.WriteLine("1. Basic Storage Usage Example");
        Console.WriteLine("-------------------------------");

        // File storage usage
        var fileStorage = new FileStorage("ExampleStorage");

        // Data writing
        var testData = Encoding.UTF8.GetBytes("Hello, StorageSharp!");
        await fileStorage.WriteAsync("test.txt", testData);
        Console.WriteLine("✓ Data written to file storage");

        // Data reading
        var readData = await fileStorage.ReadAsync("test.txt");
        var readText = Encoding.UTF8.GetString(readData);
        Console.WriteLine($"✓ Read data: {readText}");

        // Memory storage usage
        var memoryStorage = new MemoryStorage();
        await memoryStorage.WriteAsync("memory-test.txt", testData);
        Console.WriteLine("✓ Data written to memory storage");

        var memoryData = await memoryStorage.ReadAsync("memory-test.txt");
        var memoryText = Encoding.UTF8.GetString(memoryData);
        Console.WriteLine($"✓ Data read from memory: {memoryText}");
    }

    private static async Task CachedStorageExample()
    {
        Console.WriteLine("2. Cached Storage Usage Example");
        Console.WriteLine("-----------------------------------");

        // Cached storage setup
        var cache = new MemoryStorage();
        var origin = new FileStorage("OriginStorage");
        var cachedStorage = new CachedStorage(cache, origin);

        // Data writing
        var data = Encoding.UTF8.GetBytes("Cached data example");
        await cachedStorage.WriteAsync("cached-file.txt", data);
        Console.WriteLine("✓ Data written to cached storage");

        // First read (cache miss)
        var readData1 = await cachedStorage.ReadAsync("cached-file.txt");
        Console.WriteLine($"✓ First read: {Encoding.UTF8.GetString(readData1)}");

        // Second read (cache hit)
        var readData2 = await cachedStorage.ReadAsync("cached-file.txt");
        Console.WriteLine($"✓ Second read: {Encoding.UTF8.GetString(readData2)}");

        Console.WriteLine($"✓ Cache hit count: {cachedStorage.CacheHitCount}");
    }

    private static async Task ZippedPackagesExample()
    {
        Console.WriteLine("3. ZIP Packages Usage Example");
        Console.WriteLine("-------------------------");

        // Create test directory
        var testDir = "TestDirectory";
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
        Directory.CreateDirectory(testDir);

        // Create test files
        var testFile1 = Path.Combine(testDir, "file1.txt");
        var testFile2 = Path.Combine(testDir, "file2.txt");
        var subDir = Path.Combine(testDir, "subdir");
        Directory.CreateDirectory(subDir);
        var testFile3 = Path.Combine(subDir, "file3.txt");

        await File.WriteAllTextAsync(testFile1, "This is file 1");
        await File.WriteAllTextAsync(testFile2, "This is file 2");
        await File.WriteAllTextAsync(testFile3, "This is file 3 in subdir");

        Console.WriteLine("✓ Test directory and files created");

        // ZIP packages setup
        var storage = new FileStorage("ZippedPackages");
        var packages = new ZippedPacks(
            new ZippedPacks.Settings("Tmp/Packages/"),
            storage
        );

        // Add directory to archive
        var archiveScheme = await packages.Add(testDir);
        Console.WriteLine($"✓ Directory added to archive: {archiveScheme.DirectoryPath}");

        // Load archive
        var loadedPath = await packages.Load(archiveScheme);
        Console.WriteLine($"✓ Archive loaded: {loadedPath}");

        // Check loaded file
        var loadedFile1 = Path.Combine(loadedPath, "file1.txt");
        if (File.Exists(loadedFile1))
        {
            var content = await File.ReadAllTextAsync(loadedFile1);
            Console.WriteLine($"✓ Loaded file content: {content}");
        }

        // Unload archive
        await packages.Unload(archiveScheme);
        Console.WriteLine("✓ Archive unloaded");

        // Delete archive
        await packages.Delete(archiveScheme);
        Console.WriteLine("✓ Archive deleted");

        // Clean up test directory
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    }
}