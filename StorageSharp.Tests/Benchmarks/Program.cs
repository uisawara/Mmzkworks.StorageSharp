using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using StorageSharp.Tests.Benchmarks;

namespace StorageSharp.Tests.Benchmarks
{
    /// <summary>
    /// ベンチマーク実行用のプログラム
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("StorageSharp Benchmark Suite");
            Console.WriteLine("============================");
            
            // 出力ディレクトリを設定
            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkResults");
            Console.WriteLine($"Output directory: {outputDir}");
            
            // BenchmarkDotNetの設定
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .WithArtifactsPath(outputDir);
            
            try
            {
                // CI環境かどうかを判定
                var isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
                
                if (!isCI)
                {
                    // ローカル環境: ApiStorageベンチマーク（Mockサーバーが必要）
                    Console.WriteLine("\n1. Running ApiStorage benchmarks...");
                    try
                    {
                        var apiSummary = BenchmarkRunner.Run<ApiStorageBenchmarks>(config);
                        Console.WriteLine("ApiStorage benchmarks completed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: ApiStorage benchmarks failed: {ex.Message}");
                        Console.WriteLine("Make sure the mock server is running on http://localhost:8080");
                    }
                }
                else
                {
                    Console.WriteLine("\n1. Skipping ApiStorage benchmarks in CI environment (no mock server)");
                }
                
                // 2. ストレージ比較ベンチマーク
                Console.WriteLine("\n2. Running Storage comparison benchmarks...");
                var storageSummary = BenchmarkRunner.Run<StorageBenchmarks>(config);
                Console.WriteLine("Storage comparison benchmarks completed successfully.");
                
                // 3. キャッシュとストレージの組み合わせベンチマーク
                Console.WriteLine("\n3. Running Storage combination benchmarks...");
                var combinationSummary = BenchmarkRunner.Run<StorageCombinationBenchmarks>(config);
                Console.WriteLine("Storage combination benchmarks completed successfully.");
                
                Console.WriteLine("\nBenchmark execution completed successfully!");
                Console.WriteLine($"Results are available in: {outputDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during benchmark execution: {ex}");
                Environment.Exit(1);
            }
        }
    }
} 