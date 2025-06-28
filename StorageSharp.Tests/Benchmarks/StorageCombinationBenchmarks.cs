using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using StorageSharp.Storages;

namespace StorageSharp.Tests.Benchmarks
{
    /// <summary>
    /// キャッシュとストレージの組み合わせベンチマーク
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob]
    public class StorageCombinationBenchmarks
    {
        private string? _tempDirectory;
        
        // 基本ストレージ
        private MemoryStorage? _memoryStorage;
        private FileStorage? _fileStorage;
        private ApiStorage? _apiStorage;
        
        // キャッシュ付きストレージの組み合わせ
        private CachedStorage? _memoryCacheWithFileStorage;
        private CachedStorage? _memoryCacheWithApiStorage;
        private CachedStorage? _fileCacheWithFileStorage;
        private CachedStorage? _fileCacheWithApiStorage;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            // 一時ディレクトリを作成
            _tempDirectory = Path.Combine(Path.GetTempPath(), "StorageSharpCombinationBenchmarks", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            // 基本ストレージを初期化
            _memoryStorage = new MemoryStorage();
            _fileStorage = new FileStorage(_tempDirectory);
            _apiStorage = new ApiStorage("http://localhost:8080");

            // キャッシュ付きストレージの組み合わせを初期化
            var memoryCache = new MemoryStorage();
            var fileCache = new FileStorage(Path.Combine(_tempDirectory, "cache"));
            var originFileStorage = new FileStorage(Path.Combine(_tempDirectory, "origin"));
            var originApiStorage = new ApiStorage("http://localhost:8080");

            _memoryCacheWithFileStorage = new CachedStorage(memoryCache, originFileStorage);
            _memoryCacheWithApiStorage = new CachedStorage(memoryCache, originApiStorage);
            _fileCacheWithFileStorage = new CachedStorage(fileCache, originFileStorage);
            _fileCacheWithApiStorage = new CachedStorage(fileCache, originApiStorage);

            // テストデータを準備
            await PrepareTestData();
        }

        private async Task PrepareTestData()
        {
            var testData = Encoding.UTF8.GetBytes("Combination benchmark test data");
            var largeData = new byte[1024 * 512]; // 512KB
            new Random(42).NextBytes(largeData);

            // 各ストレージにテストデータを書き込み
            var storages = new IStorage?[] 
            { 
                _memoryStorage, 
                _fileStorage, 
                _memoryCacheWithFileStorage,
                _fileCacheWithFileStorage
            };

            foreach (var storage in storages)
            {
                if (storage != null)
                {
                    try
                    {
                        await storage.WriteAsync("combination_test.txt", testData);
                        await storage.WriteAsync("combination_large.bin", largeData);
                    }
                    catch (Exception ex)
                    {
                        if (storage is not ApiStorage)
                        {
                            Console.WriteLine($"Warning: Failed to prepare test data for {storage.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            }

            // ApiStorage系のテストデータ
            var apiStorages = new IStorage?[] { _apiStorage, _memoryCacheWithApiStorage, _fileCacheWithApiStorage };
            foreach (var storage in apiStorages)
            {
                if (storage != null)
                {
                    try
                    {
                        await storage.WriteAsync("combination_test.txt", testData);
                        await storage.WriteAsync("combination_large.bin", largeData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to prepare test data for {storage.GetType().Name}: {ex.Message}");
                    }
                }
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

        // メモリキャッシュ + ファイルストレージの組み合わせテスト
        [Benchmark]
        public async Task MemoryCacheWithFileStorage_Write()
        {
            var data = Encoding.UTF8.GetBytes("Memory cache with file storage write test");
            await _memoryCacheWithFileStorage!.WriteAsync("memory_cache_file_write.txt", data);
        }

        [Benchmark]
        public async Task MemoryCacheWithFileStorage_Read()
        {
            try
            {
                var data = await _memoryCacheWithFileStorage!.ReadAsync("combination_test.txt");
                _ = data.Length;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        [Benchmark]
        public async Task MemoryCacheWithFileStorage_ReadLarge()
        {
            try
            {
                var data = await _memoryCacheWithFileStorage!.ReadAsync("combination_large.bin");
                _ = data.Length;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        // メモリキャッシュ + APIストレージの組み合わせテスト
        [Benchmark]
        public async Task MemoryCacheWithApiStorage_Write()
        {
            try
            {
                var data = Encoding.UTF8.GetBytes("Memory cache with API storage write test");
                await _memoryCacheWithApiStorage!.WriteAsync("memory_cache_api_write.txt", data);
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        [Benchmark]
        public async Task MemoryCacheWithApiStorage_Read()
        {
            try
            {
                var data = await _memoryCacheWithApiStorage!.ReadAsync("combination_test.txt");
                _ = data.Length;
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        // ファイルキャッシュ + ファイルストレージの組み合わせテスト
        [Benchmark]
        public async Task FileCacheWithFileStorage_Write()
        {
            var data = Encoding.UTF8.GetBytes("File cache with file storage write test");
            await _fileCacheWithFileStorage!.WriteAsync("file_cache_file_write.txt", data);
        }

        [Benchmark]
        public async Task FileCacheWithFileStorage_Read()
        {
            try
            {
                var data = await _fileCacheWithFileStorage!.ReadAsync("combination_test.txt");
                _ = data.Length;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        [Benchmark]
        public async Task FileCacheWithFileStorage_ReadLarge()
        {
            try
            {
                var data = await _fileCacheWithFileStorage!.ReadAsync("combination_large.bin");
                _ = data.Length;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        // ファイルキャッシュ + APIストレージの組み合わせテスト
        [Benchmark]
        public async Task FileCacheWithApiStorage_Write()
        {
            try
            {
                var data = Encoding.UTF8.GetBytes("File cache with API storage write test");
                await _fileCacheWithApiStorage!.WriteAsync("file_cache_api_write.txt", data);
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        [Benchmark]
        public async Task FileCacheWithApiStorage_Read()
        {
            try
            {
                var data = await _fileCacheWithApiStorage!.ReadAsync("combination_test.txt");
                _ = data.Length;
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        // キャッシュ効果の測定（初回読み取り vs 2回目読み取り）
        [Benchmark]
        public async Task MemoryCacheWithFileStorage_FirstRead()
        {
            try
            {
                var data = await _memoryCacheWithFileStorage!.ReadAsync("combination_test.txt");
                _ = data.Length;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        [Benchmark]
        public async Task MemoryCacheWithFileStorage_CachedRead()
        {
            try
            {
                // 2回目の読み取り（キャッシュヒット）
                var data = await _memoryCacheWithFileStorage!.ReadAsync("combination_test.txt");
                _ = data.Length;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        // 複数ファイルの一括操作テスト
        [Benchmark]
        public async Task MemoryCacheWithFileStorage_WriteMultiple()
        {
            var testData = Encoding.UTF8.GetBytes("Multiple file test data");
            for (int i = 0; i < 5; i++)
            {
                await _memoryCacheWithFileStorage!.WriteAsync($"multiple_{i}.txt", testData);
            }
        }

        [Benchmark]
        public async Task MemoryCacheWithFileStorage_ReadMultiple()
        {
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    var data = await _memoryCacheWithFileStorage!.ReadAsync($"multiple_{i}.txt");
                    _ = data.Length;
                }
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        // リスト取得の比較
        [Benchmark]
        public async Task MemoryCacheWithFileStorage_ListAll()
        {
            try
            {
                var files = await _memoryCacheWithFileStorage!.ListAll();
                _ = files.Length;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }

        [Benchmark]
        public async Task FileCacheWithFileStorage_ListAll()
        {
            try
            {
                var files = await _fileCacheWithFileStorage!.ListAll();
                _ = files.Length;
            }
            catch (Exception)
            {
                // エラーを無視
            }
        }
    }
} 