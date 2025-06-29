using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace StorageSharp.Tests.Benchmarks
{
    /// <summary>
    /// ApiStorageクラスを使用したベンチマーク
    /// PythonのMockサーバーと連携してパフォーマンスを測定します
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob]
    public class ApiStorageBenchmarks : IDisposable
    {
        private Process? _serverProcess;
        private ApiStorage? _apiStorage;
        private readonly string _serverScriptPath;
        private readonly string _pythonPath;
        private bool _isServerStarted = false;

        public ApiStorageBenchmarks()
        {
            // Pythonパスを検出
            _pythonPath = FindPythonPath();
            _serverScriptPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "MockServer",
                "mock_storage_server.py"
            );

            if (!File.Exists(_serverScriptPath))
            {
                throw new FileNotFoundException($"Mock server script not found: {_serverScriptPath}");
            }
        }

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            await StartServerAsync();
            _apiStorage = new ApiStorage("http://localhost:8080");
            await WaitForServerReadyAsync(_apiStorage);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            StopServer();
            _apiStorage?.Dispose();
        }

        [Benchmark]
        public async Task Write_SmallFile()
        {
            var data = Encoding.UTF8.GetBytes("Small file content for benchmark");
            await _apiStorage!.WriteAsync("benchmark_small.txt", data);
        }

        [Benchmark]
        public async Task Read_SmallFile()
        {
            var data = await _apiStorage!.ReadAsync("benchmark_small.txt");
            _ = data.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task Write_LargeFile()
        {
            var data = new byte[1024 * 1024]; // 1MB
            new Random(42).NextBytes(data);
            await _apiStorage!.WriteAsync("benchmark_large.bin", data);
        }

        [Benchmark]
        public async Task Read_LargeFile()
        {
            var data = await _apiStorage!.ReadAsync("benchmark_large.bin");
            _ = data.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task ListAll()
        {
            var keys = await _apiStorage!.ListAll();
            _ = keys.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task Write_MultipleSmallFiles()
        {
            for (int i = 0; i < 10; i++)
            {
                var data = Encoding.UTF8.GetBytes($"Small file {i} content");
                await _apiStorage!.WriteAsync($"benchmark_multiple_{i}.txt", data);
            }
        }

        [Benchmark]
        public async Task Read_MultipleSmallFiles()
        {
            for (int i = 0; i < 10; i++)
            {
                var data = await _apiStorage!.ReadAsync($"benchmark_multiple_{i}.txt");
                _ = data.Length; // 結果を使用して最適化を防ぐ
            }
        }

        [Benchmark]
        public async Task Write_Stream()
        {
            var content = "Stream content for benchmark";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var reader = new StreamReader(stream);
            await _apiStorage!.WriteAsync("benchmark_stream.txt", reader);
        }

        [Benchmark]
        public async Task Read_Stream()
        {
            using var streamReader = await _apiStorage!.ReadToStreamAsync("benchmark_stream.txt");
            var content = await streamReader.ReadToEndAsync();
            _ = content.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task HealthCheck()
        {
            var isHealthy = await _apiStorage!.IsHealthyAsync();
            _ = isHealthy; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task Write_Concurrent()
        {
            var tasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                var index = i;
                tasks[i] = Task.Run(async () =>
                {
                    var data = Encoding.UTF8.GetBytes($"Concurrent file {index} content");
                    await _apiStorage!.WriteAsync($"benchmark_concurrent_{index}.txt", data);
                });
            }
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task Read_Concurrent()
        {
            var tasks = new Task<byte[]>[5];
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = _apiStorage!.ReadAsync($"benchmark_concurrent_{i}.txt");
            }
            var results = await Task.WhenAll(tasks);
            _ = results.Length; // 結果を使用して最適化を防ぐ
        }

        [Benchmark]
        public async Task MixedOperations()
        {
            // 書き込み
            var writeData = Encoding.UTF8.GetBytes("Mixed operations test");
            await _apiStorage!.WriteAsync("benchmark_mixed.txt", writeData);

            // 読み取り
            var readData = await _apiStorage.ReadAsync("benchmark_mixed.txt");

            // リスト取得
            var keys = await _apiStorage.ListAll();

            // 削除
            await _apiStorage.DeleteAsync("benchmark_mixed.txt");

            _ = readData.Length + keys.Length; // 結果を使用して最適化を防ぐ
        }

        private async Task StartServerAsync()
        {
            if (_isServerStarted)
            {
                return; // 既に起動中
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{_serverScriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _serverProcess = new Process { StartInfo = startInfo };
            _serverProcess.Start();

            // サーバーの起動を少し待機
            await Task.Delay(2000);
            _isServerStarted = true;
        }

        private void StopServer()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    _serverProcess.Kill();
                    _serverProcess.WaitForExit(5000);
                }
                catch
                {
                    // プロセスの終了に失敗しても無視
                }
                finally
                {
                    _serverProcess.Dispose();
                    _serverProcess = null;
                    _isServerStarted = false;
                }
            }
        }

        private async Task WaitForServerReadyAsync(ApiStorage apiStorage, int maxRetries = 10)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                if (await apiStorage.IsHealthyAsync())
                {
                    return;
                }
                await Task.Delay(1000);
            }

            throw new TimeoutException("Server did not become ready within the expected time");
        }

        private string FindPythonPath()
        {
            // 一般的なPythonパスを試行
            var possiblePaths = new[]
            {
                "python",
                "python3",
                "python.exe",
                "python3.exe"
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null && process.WaitForExit(5000) && process.ExitCode == 0)
                    {
                        return path;
                    }
                }
                catch
                {
                    // パスが見つからない場合は次を試行
                }
            }

            throw new InvalidOperationException("Python not found. Please ensure Python is installed and available in PATH.");
        }

        public void Dispose()
        {
            GlobalCleanup();
        }
    }
} 