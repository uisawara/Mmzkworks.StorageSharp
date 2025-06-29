using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using StorageSharp.Storages;

namespace StorageSharp.Tests.Benchmarks
{
    /// <summary>
    /// 異なるストレージ構成のベンチマーク
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob]
    public class StorageBenchmarks
    {
        private ApiStorage? _apiStorage;
        private MemoryStorage? _memoryStorage;
        private FileStorage? _fileStorage;
        private CachedStorage? _cachedStorage;
        private string? _tempDirectory;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            // 一時ディレクトリを作成
            _tempDirectory = Path.Combine(Path.GetTempPath(), "StorageSharpBenchmarks", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            // 各ストレージインスタンスを初期化
            _apiStorage = new ApiStorage("http://localhost:8080");
            _memoryStorage = new MemoryStorage();
            _fileStorage = new FileStorage(_tempDirectory);
            
            // キャッシュ付きストレージ（メモリキャッシュ + ファイルストレージ）
            var cacheStorage = new MemoryStorage();
            var originStorage = new FileStorage(Path.Combine(_tempDirectory, "origin"));
            _cachedStorage = new CachedStorage(cacheStorage, originStorage);

            // Read系ベンチマーク用のテストデータを事前に用意
            await PrepareTestData();
        }

        private async Task PrepareTestData()
        {
            // 小さいファイルのテストデータ
            var smallData = Encoding.UTF8.GetBytes("Small file content for benchmark");
            
            // 大きいファイルのテストデータ
            var largeData = new byte[1024 * 1024]; // 1MB
            new Random(42).NextBytes(largeData);

            // 複数ファイルのテストデータ
            var multipleData = Encoding.UTF8.GetBytes("Multiple file content for benchmark");

            // 各ストレージにテストデータを書き込み
            var storages = new IStorage?[] { _memoryStorage, _fileStorage, _cachedStorage };
            
            foreach (var storage in storages)
            {
                if (storage != null)
                {
                    try
                    {
                        // 小さいファイル
                        await storage.WriteAsync("benchmark_small.txt", smallData);
                        
                        // 大きいファイル
                        await storage.WriteAsync("benchmark_large.bin", largeData);
                        
                        // 複数ファイル（リスト取得テスト用）
                        for (int i = 0; i < 10; i++)
                        {
                            await storage.WriteAsync($"benchmark_multiple_{i}.txt", multipleData);
                        }
                    }
                    catch (Exception ex)
                    {
                        // ApiStorageはMockサーバーが起動していない場合に失敗する可能性があるため無視
                        if (storage is not ApiStorage)
                        {
                            Console.WriteLine($"Warning: Failed to prepare test data for {storage.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            }

            // ApiStorage用のテストデータ（Mockサーバーが起動している場合のみ）
            if (_apiStorage != null)
            {
                try
                {
                    await _apiStorage.WriteAsync("benchmark_small.txt", smallData);
                    await _apiStorage.WriteAsync("benchmark_large.bin", largeData);
                    for (int i = 0; i < 10; i++)
                    {
                        await _apiStorage.WriteAsync($"benchmark_multiple_{i}.txt", multipleData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to prepare test data for ApiStorage (Mock server may not be running): {ex.Message}");
                }
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _apiStorage?.Dispose();
            
            // 一時ディレクトリを削除
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

        // 小さいファイルの書き込みベンチマーク
        [Benchmark]
        public async Task ApiStorage_WriteSmall()
        {
            try
            {
                var data = Encoding.UTF8.GetBytes("Small file content for benchmark");
                await _apiStorage!.WriteAsync("benchmark_small.txt", data);
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        [Benchmark]
        public async Task MemoryStorage_WriteSmall()
        {
            var data = Encoding.UTF8.GetBytes("Small file content for benchmark");
            await _memoryStorage!.WriteAsync("benchmark_small.txt", data);
        }

        [Benchmark]
        public async Task FileStorage_WriteSmall()
        {
            var data = Encoding.UTF8.GetBytes("Small file content for benchmark");
            await _fileStorage!.WriteAsync("benchmark_small.txt", data);
        }

        [Benchmark]
        public async Task CachedStorage_WriteSmall()
        {
            var data = Encoding.UTF8.GetBytes("Small file content for benchmark");
            await _cachedStorage!.WriteAsync("benchmark_small.txt", data);
        }

        // 小さいファイルの読み取りベンチマーク
        [Benchmark]
        public async Task ApiStorage_ReadSmall()
        {
            try
            {
                var data = await _apiStorage!.ReadAsync("benchmark_small.txt");
                _ = data.Length; // 結果を使用して最適化を防ぐ
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        [Benchmark]
        public async Task MemoryStorage_ReadSmall()
        {
            var data = await _memoryStorage!.ReadAsync("benchmark_small.txt");
            _ = data.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task FileStorage_ReadSmall()
        {
            var data = await _fileStorage!.ReadAsync("benchmark_small.txt");
            _ = data.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task CachedStorage_ReadSmall()
        {
            var data = await _cachedStorage!.ReadAsync("benchmark_small.txt");
            _ = data.Length; // 結果を使用して最適化を防ぐ
        }

        // 大きいファイルの書き込みベンチマーク
        [Benchmark]
        public async Task ApiStorage_WriteLarge()
        {
            try
            {
                var data = new byte[1024 * 1024]; // 1MB
                new Random(42).NextBytes(data);
                await _apiStorage!.WriteAsync("benchmark_large.bin", data);
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        [Benchmark]
        public async Task MemoryStorage_WriteLarge()
        {
            var data = new byte[1024 * 1024]; // 1MB
            new Random(42).NextBytes(data);
            await _memoryStorage!.WriteAsync("benchmark_large.bin", data);
        }

        [Benchmark]
        public async Task FileStorage_WriteLarge()
        {
            var data = new byte[1024 * 1024]; // 1MB
            new Random(42).NextBytes(data);
            await _fileStorage!.WriteAsync("benchmark_large.bin", data);
        }

        [Benchmark]
        public async Task CachedStorage_WriteLarge()
        {
            var data = new byte[1024 * 1024]; // 1MB
            new Random(42).NextBytes(data);
            await _cachedStorage!.WriteAsync("benchmark_large.bin", data);
        }

        // 大きいファイルの読み取りベンチマーク
        [Benchmark]
        public async Task ApiStorage_ReadLarge()
        {
            try
            {
                var data = await _apiStorage!.ReadAsync("benchmark_large.bin");
                _ = data.Length; // 結果を使用して最適化を防ぐ
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        [Benchmark]
        public async Task MemoryStorage_ReadLarge()
        {
            var data = await _memoryStorage!.ReadAsync("benchmark_large.bin");
            _ = data.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task FileStorage_ReadLarge()
        {
            var data = await _fileStorage!.ReadAsync("benchmark_large.bin");
            _ = data.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task CachedStorage_ReadLarge()
        {
            var data = await _cachedStorage!.ReadAsync("benchmark_large.bin");
            _ = data.Length; // 結果を使用して最適化を防ぐ
        }

        // リスト取得ベンチマーク
        [Benchmark]
        public async Task ApiStorage_ListAll()
        {
            try
            {
                var keys = await _apiStorage!.ListAll();
                _ = keys.Length; // 結果を使用して最適化を防ぐ
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        [Benchmark]
        public async Task MemoryStorage_ListAll()
        {
            var keys = await _memoryStorage!.ListAll();
            _ = keys.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task FileStorage_ListAll()
        {
            var keys = await _fileStorage!.ListAll();
            _ = keys.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task CachedStorage_ListAll()
        {
            var keys = await _cachedStorage!.ListAll();
            _ = keys.Length; // 結果を使用して最適化を防ぐ
        }

        // 複数ファイルの書き込みベンチマーク
        [Benchmark]
        public async Task ApiStorage_WriteMultiple()
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    var data = Encoding.UTF8.GetBytes($"Multiple file {i} content");
                    await _apiStorage!.WriteAsync($"benchmark_multiple_{i}.txt", data);
                }
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        [Benchmark]
        public async Task MemoryStorage_WriteMultiple()
        {
            for (int i = 0; i < 10; i++)
            {
                var data = Encoding.UTF8.GetBytes($"Multiple file {i} content");
                await _memoryStorage!.WriteAsync($"benchmark_multiple_{i}.txt", data);
            }
        }

        [Benchmark]
        public async Task FileStorage_WriteMultiple()
        {
            for (int i = 0; i < 10; i++)
            {
                var data = Encoding.UTF8.GetBytes($"Multiple file {i} content");
                await _fileStorage!.WriteAsync($"benchmark_multiple_{i}.txt", data);
            }
        }

        [Benchmark]
        public async Task CachedStorage_WriteMultiple()
        {
            for (int i = 0; i < 10; i++)
            {
                var data = Encoding.UTF8.GetBytes($"Multiple file {i} content");
                await _cachedStorage!.WriteAsync($"benchmark_multiple_{i}.txt", data);
            }
        }

        // 複数ファイルの読み取りベンチマーク
        [Benchmark]
        public async Task ApiStorage_ReadMultiple()
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    var data = await _apiStorage!.ReadAsync($"benchmark_multiple_{i}.txt");
                    _ = data.Length; // 結果を使用して最適化を防ぐ
                }
            }
            catch (Exception)
            {
                // Mockサーバーが起動していない場合は無視
            }
        }

        [Benchmark]
        public async Task MemoryStorage_ReadMultiple()
        {
            for (int i = 0; i < 10; i++)
            {
                var data = await _memoryStorage!.ReadAsync($"benchmark_multiple_{i}.txt");
                _ = data.Length; // 結果を使用して最適化を防ぐ
            }
        }

        [Benchmark]
        public async Task FileStorage_ReadMultiple()
        {
            for (int i = 0; i < 10; i++)
            {
                var data = await _fileStorage!.ReadAsync($"benchmark_multiple_{i}.txt");
                _ = data.Length; // 結果を使用して最適化を防ぐ
            }
        }

        [Benchmark]
        public async Task CachedStorage_ReadMultiple()
        {
            for (int i = 0; i < 10; i++)
            {
                var data = await _cachedStorage!.ReadAsync($"benchmark_multiple_{i}.txt");
                _ = data.Length; // 結果を使用して最適化を防ぐ
            }
        }
    }
} 