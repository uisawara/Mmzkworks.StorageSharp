using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using StorageSharp.Storages;
using StorageSharp.Packs;
using System.Net.Http;

namespace StorageSharp.Tests.Benchmarks
{
    /// <summary>
    /// overview.mdの構成に基づくベンチマーク（軽量版）
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(
        warmupCount: 1,      // ウォームアップ回数を1回に削減
        iterationCount: 3,   // 反復回数を3回に削減
        launchCount: 1       // 起動回数を1回に削減
    )]
    public class StorageCombinationBenchmarks
    {
        private string? _tempDirectory;
        private bool _mockServerAvailable = false;
        
        // 基本ストレージ
        private FileStorage? _fileStorage;
        private MemoryStorage? _memoryStorage;
        private ApiStorage? _apiStorage;
        
        // キャッシュ付きストレージ
        private CachedStorage? _fileCache;
        private CachedStorage? _memoryCache;
        
        // ZIP展開付きストレージ
        private ZippedPacks? _fileCacheZippedPacks;
        private ZippedPacks? _memoryCacheZippedPacks;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            // 一時ディレクトリを作成
            _tempDirectory = Path.Combine(Path.GetTempPath(), "StorageSharpOverviewBenchmarks", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            // MockServerの起動状況を確認
            _mockServerAvailable = await CheckMockServerAvailability();

            // 基本ストレージを初期化
            _fileStorage = new FileStorage(_tempDirectory);
            _memoryStorage = new MemoryStorage();
            _apiStorage = new ApiStorage("http://localhost:8080");

            // キャッシュ付きストレージを初期化
            var fileCacheStorage = new FileStorage(Path.Combine(_tempDirectory, "file_cache"));
            var memoryCacheStorage = new MemoryStorage();
            var originApiStorage = new ApiStorage("http://localhost:8080");

            _fileCache = new CachedStorage(fileCacheStorage, originApiStorage);
            _memoryCache = new CachedStorage(memoryCacheStorage, originApiStorage);

            // ZIP展開付きストレージを初期化
            var fileCacheForZip = new CachedStorage(
                new FileStorage(Path.Combine(_tempDirectory, "zip_file_cache")), 
                originApiStorage);
            var memoryCacheForZip = new CachedStorage(
                new MemoryStorage(), 
                originApiStorage);

            _fileCacheZippedPacks = new ZippedPacks(
                new ZippedPacks.Settings(Path.Combine(_tempDirectory, "file_cache_extracted")),
                fileCacheForZip);
            
            _memoryCacheZippedPacks = new ZippedPacks(
                new ZippedPacks.Settings(Path.Combine(_tempDirectory, "memory_cache_extracted")),
                memoryCacheForZip);

            // テストデータを準備
            await PrepareTestData();
        }

        private async Task<bool> CheckMockServerAvailability()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetAsync("http://localhost:8080/health");
                var isHealthy = response.IsSuccessStatusCode;
                Console.WriteLine($"MockServer health check: {(isHealthy ? "OK" : "Failed")}");
                return isHealthy;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MockServer health check failed: {ex.Message}");
                return false;
            }
        }

        private async Task PrepareTestData()
        {
            var testData = Encoding.UTF8.GetBytes("Overview benchmark test data");
            var largeData = new byte[1024 * 512]; // 512KB
            new Random(42).NextBytes(largeData);

            // 基本ストレージにテストデータを書き込み
            var storages = new IStorage?[] { _fileStorage, _memoryStorage };
            foreach (var storage in storages)
            {
                if (storage != null)
                {
                    try
                    {
                        await storage.WriteAsync("overview_test.txt", testData);
                        await storage.WriteAsync("overview_large.bin", largeData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to prepare test data for {storage.GetType().Name}: {ex.Message}");
                    }
                }
            }

            // APIストレージ系のテストデータ
            if (_mockServerAvailable)
            {
                Console.WriteLine("Preparing API storage test data...");
                var apiStorages = new IStorage?[] { _apiStorage, _fileCache, _memoryCache };
                foreach (var storage in apiStorages)
                {
                    if (storage != null)
                    {
                        try
                        {
                            await storage.WriteAsync("overview_test.txt", testData);
                            await storage.WriteAsync("overview_large.bin", largeData);
                            Console.WriteLine($"Successfully prepared test data for {storage.GetType().Name}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Failed to prepare test data for {storage.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("MockServer is not available. Skipping API storage test data preparation.");
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _apiStorage?.Dispose();
            
            if (!string.IsNullOrEmpty(_tempDirectory) && Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, true);
                }
                catch
                {
                    // 削除に失敗しても無視
                }
            }
        }

        // 1. ファイルストレージ
        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task FileStorage_Write()
        {
            var data = Encoding.UTF8.GetBytes("File storage write test data");
            await _fileStorage!.WriteAsync("file_storage_test.txt", data);
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task FileStorage_FirstRead()
        {
            var data = await _fileStorage!.ReadAsync("overview_test.txt");
            _ = data.Length;
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task FileStorage_SecondRead()
        {
            var data = await _fileStorage!.ReadAsync("overview_test.txt");
            _ = data.Length;
        }

        // 2. メモリストレージ
        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task MemoryStorage_Write()
        {
            var data = Encoding.UTF8.GetBytes("Memory storage write test data");
            await _memoryStorage!.WriteAsync("memory_storage_test.txt", data);
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task MemoryStorage_FirstRead()
        {
            var data = await _memoryStorage!.ReadAsync("overview_test.txt");
            _ = data.Length;
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task MemoryStorage_SecondRead()
        {
            var data = await _memoryStorage!.ReadAsync("overview_test.txt");
            _ = data.Length;
        }

        // 3. ファイルキャッシュ
        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task FileCache_Write()
        {
            if (!_mockServerAvailable) return;
            
            var data = Encoding.UTF8.GetBytes("File cache write test data");
            await _fileCache!.WriteAsync("file_cache_test.txt", data);
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task FileCache_FirstRead()
        {
            if (!_mockServerAvailable) return;
            
            // キャッシュをクリアしてから読み取り
            await _fileCache!.ClearCache();
            var data = await _fileCache!.ReadAsync("overview_test.txt");
            _ = data.Length;
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task FileCache_SecondRead()
        {
            if (!_mockServerAvailable) return;
            
            var data = await _fileCache!.ReadAsync("overview_test.txt");
            _ = data.Length;
        }

        // 4. メモリキャッシュ
        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task MemoryCache_Write()
        {
            if (!_mockServerAvailable) return;
            
            var data = Encoding.UTF8.GetBytes("Memory cache write test data");
            await _memoryCache!.WriteAsync("memory_cache_test.txt", data);
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task MemoryCache_FirstRead()
        {
            if (!_mockServerAvailable) return;
            
            // キャッシュをクリアしてから読み取り
            await _memoryCache!.ClearCache();
            var data = await _memoryCache!.ReadAsync("overview_test.txt");
            _ = data.Length;
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task MemoryCache_SecondRead()
        {
            if (!_mockServerAvailable) return;
            
            var data = await _memoryCache!.ReadAsync("overview_test.txt");
            _ = data.Length;
        }

        // 5. ファイルキャッシュ＋ZIP展開
        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task FileCacheZippedPacks_Write()
        {
            if (!_mockServerAvailable) return;
            
            try
            {
                // テストディレクトリを作成してZIPパッケージとして追加
                var testDir = Path.Combine(_tempDirectory!, "test_package");
                Directory.CreateDirectory(testDir);
                var testData = Encoding.UTF8.GetBytes("File cache zipped packs test data");
                await File.WriteAllBytesAsync(Path.Combine(testDir, "test.txt"), testData);
                
                var scheme = await _fileCacheZippedPacks!.Add(testDir);
                _ = scheme.DirectoryPath;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task FileCacheZippedPacks_FirstRead()
        {
            if (!_mockServerAvailable) return;
            
            try
            {
                // パッケージをクリアしてから読み取り
                await _fileCacheZippedPacks!.Clear();
                
                // テストディレクトリを作成してZIPパッケージとして追加
                var testDir = Path.Combine(_tempDirectory!, "test_package_first");
                Directory.CreateDirectory(testDir);
                var testData = Encoding.UTF8.GetBytes("File cache zipped packs first read test data");
                await File.WriteAllBytesAsync(Path.Combine(testDir, "test.txt"), testData);
                
                var scheme = await _fileCacheZippedPacks!.Add(testDir);
                var extractPath = await _fileCacheZippedPacks!.Load(scheme);
                _ = extractPath;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task FileCacheZippedPacks_SecondRead()
        {
            if (!_mockServerAvailable) return;
            
            try
            {
                // 2回目の読み取り（キャッシュヒット）
                var schemes = await _fileCacheZippedPacks!.ListAll();
                if (schemes.Length > 0)
                {
                    var extractPath = await _fileCacheZippedPacks!.Load(schemes[0]);
                    _ = extractPath;
                }
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        // 6. メモリキャッシュ＋ZIP展開
        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task MemoryCacheZippedPacks_Write()
        {
            if (!_mockServerAvailable) return;
            
            try
            {
                // テストディレクトリを作成してZIPパッケージとして追加
                var testDir = Path.Combine(_tempDirectory!, "test_package_memory");
                Directory.CreateDirectory(testDir);
                var testData = Encoding.UTF8.GetBytes("Memory cache zipped packs test data");
                await File.WriteAllBytesAsync(Path.Combine(testDir, "test.txt"), testData);
                
                var scheme = await _memoryCacheZippedPacks!.Add(testDir);
                _ = scheme.DirectoryPath;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task MemoryCacheZippedPacks_FirstRead()
        {
            if (!_mockServerAvailable) return;
            
            try
            {
                // パッケージをクリアしてから読み取り
                await _memoryCacheZippedPacks!.Clear();
                
                // テストディレクトリを作成してZIPパッケージとして追加
                var testDir = Path.Combine(_tempDirectory!, "test_package_memory_first");
                Directory.CreateDirectory(testDir);
                var testData = Encoding.UTF8.GetBytes("Memory cache zipped packs first read test data");
                await File.WriteAllBytesAsync(Path.Combine(testDir, "test.txt"), testData);
                
                var scheme = await _memoryCacheZippedPacks!.Add(testDir);
                var extractPath = await _memoryCacheZippedPacks!.Load(scheme);
                _ = extractPath;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        [Benchmark]
        [InvocationCount(1)]  // 操作数を1回に制限
        public async Task MemoryCacheZippedPacks_SecondRead()
        {
            if (!_mockServerAvailable) return;
            
            try
            {
                // 2回目の読み取り（キャッシュヒット）
                var schemes = await _memoryCacheZippedPacks!.ListAll();
                if (schemes.Length > 0)
                {
                    var extractPath = await _memoryCacheZippedPacks!.Load(schemes[0]);
                    _ = extractPath;
                }
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }
    }
} 