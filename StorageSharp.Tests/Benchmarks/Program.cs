using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Exporters.Csv;
using StorageSharp.Tests.Benchmarks;

namespace StorageSharp.Tests.Benchmarks
{
    /// <summary>
    /// ベンチマーク実行用のプログラム
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // 出力ディレクトリを指定
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .WithArtifactsPath(Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkResults"))
                .AddExporter(new CustomHtmlExporter())
                .AddExporter(MarkdownExporter.GitHub);

            // ベンチマークを実行
            BenchmarkRunner.Run<StorageCombinationBenchmarks>(config);
            
            Console.WriteLine("Storage combination benchmarks completed successfully.");
        }
    }
} 