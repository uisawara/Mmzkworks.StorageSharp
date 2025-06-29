using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Loggers;
using System.Text;

namespace StorageSharp.Tests.Benchmarks
{
    public class CustomHtmlExporter : IExporter
    {
        public string Name => "CustomHtml";
        public string Extension => "html";
        public string FileNameSuffix => "-report";

        public void ExportToFile(Summary summary, string logFilePath)
        {
            var htmlContent = GenerateHtmlReport(summary);
            File.WriteAllText(logFilePath, htmlContent, Encoding.UTF8);
        }

        public void ExportToLog(Summary summary, ILogger logger)
        {
            // ログ出力は不要
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger logger)
        {
            var filePath = Path.Combine(summary.ResultsDirectoryPath, $"{summary.Title}{FileNameSuffix}.{Extension}");
            ExportToFile(summary, filePath);
            return new[] { filePath };
        }

        private string GenerateHtmlReport(Summary summary)
        {
            var sb = new StringBuilder();
            
            // HTMLヘッダー
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8' />");
            sb.AppendLine($"<title>{summary.Title}</title>");
            sb.AppendLine();
            sb.AppendLine("<style type=\"text/css\">");
            sb.AppendLine("	table { border-collapse: collapse; display: block; width: 100%; overflow: auto; }");
            sb.AppendLine("	td, th { padding: 6px 13px; border: 1px solid #ddd; text-align: right; position: relative; }");
            sb.AppendLine("	tr { background-color: #fff; border-top: 1px solid #ccc; }");
            sb.AppendLine("	tr:nth-child(even) { background: #f8f8f8; }");
            sb.AppendLine();
            sb.AppendLine("	/* 棒グラフ背景のスタイル */");
            sb.AppendLine("	.bar-cell {");
            sb.AppendLine("		position: relative;");
            sb.AppendLine("		z-index: 1;");
            sb.AppendLine("		overflow: hidden;");
            sb.AppendLine("	}");
            sb.AppendLine();
            sb.AppendLine("	.bar-background {");
            sb.AppendLine("		position: absolute;");
            sb.AppendLine("		top: 0;");
            sb.AppendLine("		left: 0;");
            sb.AppendLine("		height: 100%;");
            sb.AppendLine("		background: linear-gradient(90deg, rgba(52, 152, 219, 0.3) 0%, rgba(52, 152, 219, 0.1) 100%);");
            sb.AppendLine("		z-index: -1;");
            sb.AppendLine("		border-radius: 2px;");
            sb.AppendLine("		min-width: 2px;");
            sb.AppendLine("	}");
            sb.AppendLine();
            sb.AppendLine("	/* カテゴリ別の色分け */");
            sb.AppendLine("	.bar-read { background: linear-gradient(90deg, rgba(46, 204, 113, 0.3) 0%, rgba(46, 204, 113, 0.1) 100%); }");
            sb.AppendLine("	.bar-write { background: linear-gradient(90deg, rgba(231, 76, 60, 0.3) 0%, rgba(231, 76, 60, 0.1) 100%); }");
            sb.AppendLine("	.bar-firstread { background: linear-gradient(90deg, rgba(155, 89, 182, 0.3) 0%, rgba(155, 89, 182, 0.1) 100%); }");
            sb.AppendLine("	.bar-secondread { background: linear-gradient(90deg, rgba(52, 73, 94, 0.3) 0%, rgba(52, 73, 94, 0.1) 100%); }");
            sb.AppendLine("	.bar-gc { background: linear-gradient(90deg, rgba(26, 188, 156, 0.3) 0%, rgba(26, 188, 156, 0.1) 100%); }");
            sb.AppendLine();
            sb.AppendLine("	/* ホバー効果 */");
            sb.AppendLine("	.bar-cell:hover .bar-background {");
            sb.AppendLine("		opacity: 0.8;");
            sb.AppendLine("		transition: opacity 0.2s;");
            sb.AppendLine("	}");
            sb.AppendLine();
            sb.AppendLine("	/* 凡例 */");
            sb.AppendLine("	.legend {");
            sb.AppendLine("		margin: 20px 0;");
            sb.AppendLine("		padding: 15px;");
            sb.AppendLine("		background: #f8f9fa;");
            sb.AppendLine("		border-radius: 5px;");
            sb.AppendLine("		border: 1px solid #dee2e6;");
            sb.AppendLine("	}");
            sb.AppendLine();
            sb.AppendLine("	.legend-item {");
            sb.AppendLine("		display: inline-block;");
            sb.AppendLine("		margin-right: 20px;");
            sb.AppendLine("		margin-bottom: 10px;");
            sb.AppendLine("	}");
            sb.AppendLine();
            sb.AppendLine("	.legend-color {");
            sb.AppendLine("		display: inline-block;");
            sb.AppendLine("		width: 20px;");
            sb.AppendLine("		height: 20px;");
            sb.AppendLine("		margin-right: 5px;");
            sb.AppendLine("		border-radius: 3px;");
            sb.AppendLine("		vertical-align: middle;");
            sb.AppendLine("	}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // ベンチマーク情報
            sb.AppendLine("<pre><code>");
            sb.AppendLine(summary.HostEnvironmentInfo.ToString());
            sb.AppendLine("</code></pre>");
            sb.AppendLine("<pre><code></code></pre>");
            sb.AppendLine();

            // 凡例
            sb.AppendLine("<div class=\"legend\">");
            sb.AppendLine("    <strong>Performance Categories:</strong>");
            sb.AppendLine("    <div class=\"legend-item\">");
            sb.AppendLine("        <span class=\"legend-color bar-read\"></span>Read Operations");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div class=\"legend-item\">");
            sb.AppendLine("        <span class=\"legend-color bar-write\"></span>Write Operations");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div class=\"legend-item\">");
            sb.AppendLine("        <span class=\"legend-color bar-firstread\"></span>First Read (Cache Miss)");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div class=\"legend-item\">");
            sb.AppendLine("        <span class=\"legend-color bar-secondread\"></span>Second Read (Cache Hit)");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div class=\"legend-item\">");
            sb.AppendLine("        <span class=\"legend-color bar-gc\"></span>GC Collections (Gen0/Gen1/Gen2)");
            sb.AppendLine("    </div>");
            sb.AppendLine("</div>");
            sb.AppendLine();

            // テーブルヘッダー
            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr><th>Method</th><th>Mean</th><th>Error</th><th>StdDev</th><th>Median</th><th>Gen0</th><th>Gen1</th><th>Gen2</th><th>Allocated</th></tr></thead>");
            sb.AppendLine("<tbody>");

            // テーブルデータ
            foreach (var report in summary.Reports)
            {
                var benchmark = report.BenchmarkCase;
                var result = report.ResultStatistics;
                var gcStats = report.GcStats;

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{benchmark.Descriptor.WorkloadMethod.Name}</td>");
                sb.AppendLine($"<td class=\"bar-cell\">{result?.Mean.ToString("N2")} ns</td>");
                sb.AppendLine($"<td>{result?.StandardError.ToString("N2")} ns</td>");
                sb.AppendLine($"<td>{result?.StandardDeviation.ToString("N2")} ns</td>");
                sb.AppendLine($"<td>{result?.Median.ToString("N2")} ns</td>");
                sb.AppendLine($"<td class=\"bar-cell\">{gcStats.Gen0Collections:F4}</td>");
                sb.AppendLine($"<td class=\"bar-cell\">{gcStats.Gen1Collections:F4}</td>");
                sb.AppendLine($"<td class=\"bar-cell\">{gcStats.Gen2Collections:F4}</td>");
                sb.AppendLine($"<td>-</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
            sb.AppendLine();

            // JavaScript
            sb.AppendLine("<script>");
            sb.AppendLine("// ベンチマークデータを解析して棒グラフを生成");
            sb.AppendLine("function createBarCharts() {");
            sb.AppendLine("    console.log('Creating bar charts...');");
            sb.AppendLine("    const table = document.querySelector('table');");
            sb.AppendLine("    if (!table) {");
            sb.AppendLine("        console.error('Table not found');");
            sb.AppendLine("        return;");
            sb.AppendLine("    }");
            sb.AppendLine("    const rows = table.querySelectorAll('tbody tr');");
            sb.AppendLine("    const barCells = table.querySelectorAll('.bar-cell');");
            sb.AppendLine("    console.log('Found', barCells.length, 'bar cells');");
            sb.AppendLine();
            sb.AppendLine("    // 最大値を計算（APIストレージの異常値を除外）");
            sb.AppendLine("    let maxValue = 0;");
            sb.AppendLine("    let maxGen0 = 0;");
            sb.AppendLine("    let maxGen1 = 0;");
            sb.AppendLine("    let maxGen2 = 0;");
            sb.AppendLine("    const values = [];");
            sb.AppendLine();
            sb.AppendLine("    barCells.forEach((cell, index) => {");
            sb.AppendLine("        const text = cell.textContent.trim();");
            sb.AppendLine("        const rowIndex = Math.floor(index / 4); // 4つのbar-cellがあるため");
            sb.AppendLine("        const colIndex = index % 4; // 0: Mean, 1: Gen0, 2: Gen1, 3: Gen2");
            sb.AppendLine();
            sb.AppendLine("        if (colIndex === 0) { // Mean列");
            sb.AppendLine("            let value = 0;");
            sb.AppendLine();
            sb.AppendLine("            if (text.includes('ns')) {");
            sb.AppendLine("                value = parseFloat(text.replace(/[^\\d.]/g, ''));");
            sb.AppendLine("            } else if (text.includes('μs')) {");
            sb.AppendLine("                value = parseFloat(text.replace(/[^\\d.]/g, '')) * 1000;");
            sb.AppendLine("            } else if (text.includes('ms')) {");
            sb.AppendLine("                value = parseFloat(text.replace(/[^\\d.]/g, '')) * 1000000;");
            sb.AppendLine("            } else if (text.includes('s')) {");
            sb.AppendLine("                value = parseFloat(text.replace(/[^\\d.]/g, '')) * 1000000000;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            // APIストレージの異常値（4秒以上）を除外");
            sb.AppendLine("            if (value < 1000000000) { // 1秒未満のみ");
            sb.AppendLine("                values.push(value);");
            sb.AppendLine("                if (value > maxValue) {");
            sb.AppendLine("                    maxValue = value;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        } else { // Gen0, Gen1, Gen2列");
            sb.AppendLine("            const value = parseFloat(text) || 0;");
            sb.AppendLine("            if (colIndex === 1 && value > maxGen0) maxGen0 = value;");
            sb.AppendLine("            if (colIndex === 2 && value > maxGen1) maxGen1 = value;");
            sb.AppendLine("            if (colIndex === 3 && value > maxGen2) maxGen2 = value;");
            sb.AppendLine("        }");
            sb.AppendLine("    });");
            sb.AppendLine();
            sb.AppendLine("    console.log('Max values:', { maxValue, maxGen0, maxGen1, maxGen2 });");
            sb.AppendLine();
            sb.AppendLine("    // 棒グラフを生成");
            sb.AppendLine("    barCells.forEach((cell, index) => {");
            sb.AppendLine("        const text = cell.textContent.trim();");
            sb.AppendLine("        const rowIndex = Math.floor(index / 4);");
            sb.AppendLine("        const colIndex = index % 4;");
            sb.AppendLine("        let value = 0;");
            sb.AppendLine("        let maxVal = 0;");
            sb.AppendLine();
            sb.AppendLine("        if (colIndex === 0) { // Mean列");
            sb.AppendLine("            if (text.includes('ns')) {");
            sb.AppendLine("                value = parseFloat(text.replace(/[^\\d.]/g, ''));");
            sb.AppendLine("            } else if (text.includes('μs')) {");
            sb.AppendLine("                value = parseFloat(text.replace(/[^\\d.]/g, '')) * 1000;");
            sb.AppendLine("            } else if (text.includes('ms')) {");
            sb.AppendLine("                value = parseFloat(text.replace(/[^\\d.]/g, '')) * 1000000;");
            sb.AppendLine("            } else if (text.includes('s')) {");
            sb.AppendLine("                value = parseFloat(text.replace(/[^\\d.]/g, '')) * 1000000000;");
            sb.AppendLine("            }");
            sb.AppendLine("            maxVal = maxValue;");
            sb.AppendLine("        } else { // Gen0, Gen1, Gen2列");
            sb.AppendLine("            value = parseFloat(text) || 0;");
            sb.AppendLine("            if (colIndex === 1) maxVal = maxGen0;");
            sb.AppendLine("            if (colIndex === 2) maxVal = maxGen1;");
            sb.AppendLine("            if (colIndex === 3) maxVal = maxGen2;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // 棒グラフの幅を計算");
            sb.AppendLine("        let width = 0;");
            sb.AppendLine("        if (colIndex === 0 && value >= 1000000000) { // 異常値は最大幅");
            sb.AppendLine("            width = 100;");
            sb.AppendLine("        } else if (maxVal > 0) {");
            sb.AppendLine("            width = (value / maxVal) * 100;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // カテゴリを判定");
            sb.AppendLine("        const methodName = rows[rowIndex].cells[0].textContent.trim();");
            sb.AppendLine("        let category = 'default';");
            sb.AppendLine();
            sb.AppendLine("        if (methodName.includes('FirstRead')) {");
            sb.AppendLine("            category = 'firstread';");
            sb.AppendLine("        } else if (methodName.includes('SecondRead')) {");
            sb.AppendLine("            category = 'secondread';");
            sb.AppendLine("        } else if (methodName.includes('Write')) {");
            sb.AppendLine("            category = 'write';");
            sb.AppendLine("        } else if (methodName.includes('Read')) {");
            sb.AppendLine("            category = 'read';");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // Gen列の場合は特別な色を使用");
            sb.AppendLine("        if (colIndex > 0) {");
            sb.AppendLine("            category = 'gc';");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // 棒グラフ要素を作成");
            sb.AppendLine("        const bar = document.createElement('div');");
            sb.AppendLine("        bar.className = `bar-background bar-${category}`;");
            sb.AppendLine("        bar.style.width = `${width}%`;");
            sb.AppendLine();
            sb.AppendLine("        // ツールチップを追加");
            sb.AppendLine("        const columnName = colIndex === 0 ? 'Mean' : colIndex === 1 ? 'Gen0' : colIndex === 2 ? 'Gen1' : 'Gen2';");
            sb.AppendLine("        bar.title = `${columnName}: ${text} (${width.toFixed(1)}% of max)`;");
            sb.AppendLine();
            sb.AppendLine("        cell.appendChild(bar);");
            sb.AppendLine("        console.log(`Added bar for ${methodName} ${columnName}: ${width.toFixed(1)}%`);");
            sb.AppendLine("    });");
            sb.AppendLine();
            sb.AppendLine("    console.log('Bar charts created successfully');");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("// ページ読み込み時に実行");
            sb.AppendLine("window.onload = function() {");
            sb.AppendLine("    console.log('Page loaded, creating bar charts...');");
            sb.AppendLine("    createBarCharts();");
            sb.AppendLine("};");
            sb.AppendLine();
            sb.AppendLine("// DOMContentLoadedでも実行（念のため）");
            sb.AppendLine("document.addEventListener('DOMContentLoaded', function() {");
            sb.AppendLine("    console.log('DOM loaded, creating bar charts...');");
            sb.AppendLine("    createBarCharts();");
            sb.AppendLine("});");
            sb.AppendLine("</script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
} 