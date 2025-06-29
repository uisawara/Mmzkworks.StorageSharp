using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StorageSharp.Tests.IntegrationTests
{
    /// <summary>
    /// ApiStorageクラスを使用したE2Eテスト
    /// PythonのMockサーバーと連携してテストを実行します
    /// </summary>
    public class ApiStorageE2ETests : IDisposable
    {
        private Process? _serverProcess;
        private ApiStorage? _apiStorage;
        private readonly string _serverScriptPath;
        private readonly string _pythonPath;

        public ApiStorageE2ETests()
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

        public void Dispose()
        {
            StopServer();
            _apiStorage?.Dispose();
        }

        [Fact]
        public async Task ApiStorage_BasicOperations()
        {
            // サーバーを起動
            await StartServerAsync();

            // ApiStorageインスタンスを作成
            _apiStorage = new ApiStorage("http://localhost:8080");

            // サーバーの起動を待機
            await WaitForServerReadyAsync(_apiStorage);

            var testData = Encoding.UTF8.GetBytes("E2E test data");
            var key = "e2e_test.txt";

            // 書き込みテスト
            await _apiStorage.WriteAsync(key, testData);

            // 読み取りテスト
            var readData = await _apiStorage.ReadAsync(key);
            Assert.Equal(testData, readData);

            // 一覧取得テスト
            var keys = await _apiStorage.ListAll();
            Assert.Contains(key, keys);
        }

        [Fact]
        public async Task ApiStorage_StreamOperations()
        {
            // サーバーを起動
            await StartServerAsync();

            // ApiStorageインスタンスを作成
            _apiStorage = new ApiStorage("http://localhost:8080");

            // サーバーの起動を待機
            await WaitForServerReadyAsync(_apiStorage);

            var testData = "Stream E2E test data";
            var key = "stream_e2e_test.txt";

            // StreamReaderを使用した書き込みテスト
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(testData)))
            using (var reader = new StreamReader(stream))
            {
                await _apiStorage.WriteAsync(key, reader);
            }

            // StreamReaderを使用した読み取りテスト
            using (var streamReader = await _apiStorage.ReadToStreamAsync(key))
            {
                var readData = await streamReader.ReadToEndAsync();
                Assert.Equal(testData, readData);
            }
        }

        [Fact]
        public async Task ApiStorage_MultipleFiles()
        {
            // サーバーを起動
            await StartServerAsync();

            // ApiStorageインスタンスを作成
            _apiStorage = new ApiStorage("http://localhost:8080");

            // サーバーの起動を待機
            await WaitForServerReadyAsync(_apiStorage);

            // 複数のファイルを書き込み
            var files = new[]
            {
                ("file1.txt", "Content 1"),
                ("file2.txt", "Content 2"),
                ("file3.txt", "Content 3")
            };

            foreach (var (key, content) in files)
            {
                var data = Encoding.UTF8.GetBytes(content);
                await _apiStorage.WriteAsync(key, data);
            }

            // 一覧取得して確認
            var keys = await _apiStorage.ListAll();
            Assert.Equal(3, keys.Length);
            Assert.Contains("file1.txt", keys);
            Assert.Contains("file2.txt", keys);
            Assert.Contains("file3.txt", keys);

            // 各ファイルの内容を確認
            foreach (var (key, expectedContent) in files)
            {
                var data = await _apiStorage.ReadAsync(key);
                var content = Encoding.UTF8.GetString(data);
                Assert.Equal(expectedContent, content);
            }
        }

        [Fact]
        public async Task ApiStorage_DeleteOperation()
        {
            // サーバーを起動
            await StartServerAsync();

            // ApiStorageインスタンスを作成
            _apiStorage = new ApiStorage("http://localhost:8080");

            // サーバーの起動を待機
            await WaitForServerReadyAsync(_apiStorage);

            var testData = Encoding.UTF8.GetBytes("Delete test data");
            var key = "delete_test.txt";

            // ファイルを書き込み
            await _apiStorage.WriteAsync(key, testData);

            // 削除前の一覧確認
            var keysBefore = await _apiStorage.ListAll();
            Assert.Contains(key, keysBefore);

            // 削除実行
            await _apiStorage.DeleteAsync(key);

            // 削除後の一覧確認
            var keysAfter = await _apiStorage.ListAll();
            Assert.DoesNotContain(key, keysAfter);
        }

        [Fact]
        public async Task ApiStorage_LargeData()
        {
            // サーバーを起動
            await StartServerAsync();

            // ApiStorageインスタンスを作成
            _apiStorage = new ApiStorage("http://localhost:8080");

            // サーバーの起動を待機
            await WaitForServerReadyAsync(_apiStorage);

            // 大きなデータを作成（1MB）
            var largeData = new byte[1024 * 1024];
            new Random().NextBytes(largeData);
            var key = "large_file.bin";

            // 大きなファイルの書き込み
            await _apiStorage.WriteAsync(key, largeData);

            // 大きなファイルの読み取り
            var readData = await _apiStorage.ReadAsync(key);
            Assert.Equal(largeData, readData);
        }

        [Fact]
        public async Task ApiStorage_ConcurrentAccess()
        {
            // サーバーを起動
            await StartServerAsync();

            // ApiStorageインスタンスを作成
            _apiStorage = new ApiStorage("http://localhost:8080");

            // サーバーの起動を待機
            await WaitForServerReadyAsync(_apiStorage);

            var tasks = new List<Task>();

            // 並行して複数のファイルを操作
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    var data = Encoding.UTF8.GetBytes($"Concurrent test {index}");
                    var key = $"concurrent_{index}.txt";

                    await _apiStorage.WriteAsync(key, data);
                    var readData = await _apiStorage.ReadAsync(key);
                    Assert.Equal(data, readData);
                }));
            }

            await Task.WhenAll(tasks);

            // 最終的な一覧確認
            var keys = await _apiStorage.ListAll();
            Assert.Equal(10, keys.Length);
        }

        [Fact]
        public async Task ApiStorage_ErrorHandling()
        {
            // サーバーを起動
            await StartServerAsync();

            // ApiStorageインスタンスを作成
            _apiStorage = new ApiStorage("http://localhost:8080");

            // サーバーの起動を待機
            await WaitForServerReadyAsync(_apiStorage);

            // 存在しないファイルの読み取りでエラーが発生することを確認
            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await _apiStorage.ReadAsync("nonexistent.txt");
            });

            // 存在しないファイルの削除でエラーが発生することを確認
            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await _apiStorage.DeleteAsync("nonexistent.txt");
            });
        }

        [Fact]
        public async Task ApiStorage_ServerUnavailable()
        {
            // サーバーを起動せずにApiStorageインスタンスを作成
            _apiStorage = new ApiStorage("http://localhost:8080");

            // サーバーが利用できない場合のエラーハンドリングを確認
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _apiStorage.ListAll();
            });
        }

        private async Task StartServerAsync()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
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
    }
} 