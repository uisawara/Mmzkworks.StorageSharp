# StorageSharp

StorageSharpは、単独バイナリファイルとフォルダファイル集合を扱うための柔軟なストレージシステムです。
組み合わせることでファイルシステムとキャッシュ、フォルダファイル集合(Packと呼んでいます)を柔軟に扱うことができます。

## 機能

### ストレージ機能 (IStorage)

- **FileStorage**: ファイルシステムベースのストレージ
- **MemoryStorage**: メモリベースのストレージ
- **CachedStorage**: キャッシュ機能付きストレージ

### アーカイブ機能 (IPacks)

- **ZippedPacks**: ZIP形式でパッケージを管理するアーカイブ実装

## 基本的な使用方法

### ストレージの使用

```csharp
// ファイルストレージ
var fileStorage = new FileStorage("StorageDirectory");

// データの書き込み
await fileStorage.WriteAsync("key.txt", data);

// データの読み込み
var data = await fileStorage.ReadAsync("key.txt");
```

### キャッシュ付きストレージの使用

```csharp
var storage = new CachedStorage(
    cache: new MemoryStorage(), // キャッシュ用ストレージ
    origin: new FileStorage("OriginStorage") // オリジンストレージ
);
```

### ZIPパッケージの使用

```csharp
var packages = new ZippedPacks(
    new ZippedPacks.Settings("Tmp/Packs/"),
    storage
);

// ディレクトリをアーカイブに追加
var archiveScheme = await packages.Add(directoryPath);

// アーカイブをロード
var loadedPath = await packages.Load(archiveScheme);

// ファイルを使用
// ...

// アーカイブをアンロード
await packages.Unload(archiveScheme);

// アーカイブを削除
await packages.Delete(archiveScheme);

// すべてのアーカイブをリストアップ
var list = await packages.ListAll();
```

## セットアップ

### ライブラリとして使用

```bash
# プロジェクトに参照を追加
dotnet add reference path/to/StorageSharp.csproj
```

### NuGetパッケージとして使用（将来的に）

```bash
dotnet add package StorageSharp
```

### 開発環境のセットアップ

```bash
# リポジトリをクローン
git clone <repository-url>
cd storageSharp

# 依存関係を復元
dotnet restore

# ビルド
dotnet build

# テスト実行
dotnet test
```

### サンプルプログラムの実行

```bash
# サンプルプロジェクトを実行
cd StorageSharp.Samples
dotnet run
```

## プロジェクト構造

```
storageSharp/
├── StorageSharp/                    # メインライブラリ
│   ├── Storages/
│   │   ├── IStorage.cs              # ストレージインターフェース
│   │   ├── FileStorage.cs           # ファイルストレージ実装
│   │   ├── MemoryStorage.cs         # メモリストレージ実装
│   │   └── CachedStorage.cs         # キャッシュ付きストレージ実装
│   ├── Packs/
│   │   ├── IPacks.cs                # アーカイブインターフェース
│   │   └── ZippedPacks.cs           # ZIPパッケージ実装
│   └── StorageSharp.csproj          # ライブラリプロジェクト
├── StorageSharp.Samples/            # サンプルプロジェクト
│   ├── Program.cs                   # サンプルプログラム
│   ├── StorageSharp.Samples.csproj  # サンプルプロジェクト
│   └── README.md                    # サンプル用README
├── StorageSharp.Tests/              # テストプロジェクト
│   ├── UnitTests/                   # ユニットテスト
│   └── IntegrationTests/            # 統合テスト
├── storageSharp.sln                 # ソリューションファイル
└── README.md                        # このファイル
```

## 使用例

### 基本的なストレージ操作

```csharp
// ファイルストレージの使用
var fileStorage = new FileStorage("ExampleStorage");
var testData = System.Text.Encoding.UTF8.GetBytes("Hello, StorageSharp!");
await fileStorage.WriteAsync("test.txt", testData);

// メモリストレージの使用
var memoryStorage = new MemoryStorage();
await memoryStorage.WriteAsync("memory-test.txt", testData);
```

### キャッシュ付きストレージの使用

```csharp
var cache = new MemoryStorage();
var origin = new FileStorage("OriginStorage");
var cachedStorage = new CachedStorage(cache, origin);

// データの書き込み
var data = System.Text.Encoding.UTF8.GetBytes("Cached data example");
await cachedStorage.WriteAsync("cached-file.txt", data);

// 読み込み（キャッシュヒット/ミスが自動管理される）
var readData = await cachedStorage.ReadAsync("cached-file.txt");
```

### ZIPパッケージの使用

```csharp
var storage = new FileStorage("ZippedPacks");
var packages = new ZippedPacks(
    new ZippedPacks.Settings("Tmp/Packs/"),
    storage
);

// ディレクトリをアーカイブに追加
var archiveScheme = await packages.Add("MyDirectory");

// アーカイブをロードして使用
var loadedPath = await packages.Load(archiveScheme);
// ファイルを使用...
await packages.Unload(archiveScheme);

// アーカイブを削除
await packages.Delete(archiveScheme);
```

## 注意事項

- 一時ファイルは自動的に管理されますが、大量のデータを扱う場合は適切なクリーンアップを考慮してください
- キャッシュ機能はメモリ使用量に注意して使用してください
- ZIPパッケージ機能はSharpZipLibライブラリを使用しています

## 生成AIの利用について

- 本repoはChatGPT, Cursorによる生成コードを含みます。

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。 