# StorageSharp Benchmarks

StorageSharpのベンチマーク実行とグラフ生成を行うプロジェクトです。

## 概要

このプロジェクトでは以下の機能を提供します：

1. **ベンチマーク実行**: 異なるストレージ構成でのパフォーマンス測定
2. **CSV出力**: ベンチマーク結果をCSV形式で出力
3. **グラフ生成**: Pythonを使用してグラフ画像（PNG）を生成
4. **CI連携**: GitHub Actionsで自動実行とレポート生成

## 必要な環境

### .NET
- .NET 8.0 以上

### Python
- Python 3.11 以上
- 必要なパッケージ: `pandas`, `matplotlib`, `numpy`

## 使用方法

### 1. ローカル実行

```bash
# ベンチマーク実行
cd StorageSharp.Tests/Benchmarks
dotnet run

# Python依存関係のインストール
pip install -r requirements.txt

# グラフ生成
python generate_benchmark_charts.py
```

### 2. CI実行

GitHub Actionsで自動実行されます：

- `main`ブランチへのプッシュ
- `develop`ブランチへのプッシュ
- プルリクエスト作成時
- 手動実行（workflow_dispatch）

## 出力ファイル

### CSVファイル
- `ApiStorage_Results.csv`: ApiStorageのベンチマーク結果
- `StorageComparison_Results.csv`: ストレージ比較のベンチマーク結果
- `Benchmark_Comparison.csv`: 全構成の比較結果

### グラフ画像（PNG）
- `performance_comparison.png`: パフォーマンス比較グラフ
- `memory_usage_comparison.png`: メモリ使用量比較グラフ
- `response_time_comparison.png`: 応答時間比較グラフ
- `benchmark_summary.png`: 全メトリクスのサマリー

### その他
- `Benchmark_Report.html`: インタラクティブなHTMLレポート
- `Benchmark_Summary.txt`: テキスト形式のサマリー
- `ci_results.json`: CI用の結果データ

## ベンチマーク対象

### ストレージ構成
- **ApiStorage**: HTTP API経由のストレージ（Mockサーバー使用）
- **MemoryStorage**: メモリ内ストレージ
- **FileStorage**: ファイルシステムストレージ
- **CachedStorage**: キャッシュ付きストレージ（メモリ + ファイル）

### ベンチマーク項目
- **小さいファイル操作**: テキストファイルの読み書き
- **大きいファイル操作**: 1MBバイナリファイルの読み書き
- **リスト取得**: 全キーの取得
- **複数ファイル操作**: 10個のファイルの一括操作

## CI成果物

GitHub Actionsで以下の成果物が生成されます：

1. **benchmark-results**: CSV、テキスト、JSONファイル
2. **benchmark-charts**: PNGグラフ画像
3. **benchmark-html-report**: HTMLレポート

## カスタマイズ

### グラフのカスタマイズ

`generate_benchmark_charts.py`を編集することで、以下のカスタマイズが可能です：

- グラフの色やスタイル
- グラフサイズ
- フォント設定
- 出力形式（PNG、SVG、PDF等）

### ベンチマークの追加

`StorageBenchmarks.cs`に新しいベンチマークメソッドを追加することで、新しいテストケースを追加できます。

## トラブルシューティング

### Mockサーバーが起動していない場合
ApiStorageのベンチマークが失敗する場合は、Mockサーバーが起動していることを確認してください：

```bash
cd StorageSharp.Tests/MockServer
python mock_storage_server.py
```

### Python依存関係のエラー
```bash
pip install --upgrade pip
pip install -r requirements.txt
```

### グラフ生成エラー
- CSVファイルが存在することを確認
- matplotlibのバックエンド設定を確認
- 日本語フォントが利用可能か確認 