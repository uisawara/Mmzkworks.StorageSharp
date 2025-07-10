# StorageSharp

StorageSharpは、単独バイナリファイルとフォルダファイル集合を扱うための柔軟なストレージシステムです。
組み合わせることでファイルシステムとキャッシュ、フォルダファイル集合(Packと呼んでいます)を柔軟に扱うことができます。

[![CI/CD Pipeline](https://github.com/uisawara/storageSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/uisawara/storageSharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Mmzkworks.StorageSharp.svg)](https://www.nuget.org/packages/Mmzkworks.StorageSharp/)

## 読み物

- [overview](./Documents/overview.md)

## 機能

### ストレージ機能 (IStorage)

- **FileStorage**: ファイルシステムベースのストレージ
- **MemoryStorage**: メモリベースのストレージ
- **CachedStorage**: キャッシュ機能付きストレージ
- **EncryptedStorage**: 任意のIStorage実装をラップするAES-256暗号化ストレージ
- **StorageRouter**: キーのパターンに基づいて異なるストレージに操作をルーティング

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

### 暗号化ストレージの使用

```csharp
// パスワードでデータを暗号化
var baseStorage = new FileStorage("EncryptedStorage");
var encryptedStorage = new EncryptedStorage(baseStorage, "あなたの安全なパスワード");

// 暗号化されたデータの書き込み
await encryptedStorage.WriteAsync("secret.txt", data);

// データの読み込みと復号化
var decryptedData = await encryptedStorage.ReadAsync("secret.txt");

// 高度な使用法：カスタム暗号化キーとIVを使用
var key = new byte[32]; // AES-256用の32バイトキー
var iv = new byte[16];  // 16バイトIV
// キーとIVを安全な乱数で初期化...
var customEncryptedStorage = new EncryptedStorage(baseStorage, key, iv);
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
│   │   ├── CachedStorage.cs         # キャッシュ付きストレージ実装
│   │   ├── EncryptedStorage.cs      # AES-256暗号化ストレージ実装
│   │   └── StorageRouter.cs         # ストレージルーティング実装
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

### 暗号化ストレージの使用

```csharp
// パスワード付き基本暗号化ストレージ
var baseStorage = new FileStorage("SecureStorage");
var encryptedStorage = new EncryptedStorage(baseStorage, "MySecurePassword123!");

// 暗号化データの書き込み
var sensitiveData = System.Text.Encoding.UTF8.GetBytes("このデータは暗号化されます");
await encryptedStorage.WriteAsync("confidential.txt", sensitiveData);

// 暗号化データの読み込み（自動的に復号化される）
var decryptedData = await encryptedStorage.ReadAsync("confidential.txt");

// 異なるバックエンドでの暗号化ストレージ使用
var memoryBackend = new MemoryStorage();
var encryptedMemory = new EncryptedStorage(memoryBackend, "MemoryPassword");

var cachedBackend = new CachedStorage(new MemoryStorage(), new FileStorage("Cache"));
var encryptedCached = new EncryptedStorage(cachedBackend, "CachedPassword");

// カスタム暗号化設定
var customKey = new byte[32]; // AES-256用の32バイトキー
var customIV = new byte[16];  // AES用の16バイトIV
// 暗号論的に安全な乱数で埋める...
var customEncrypted = new EncryptedStorage(baseStorage, customKey, customIV);
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

### ストレージルーターの使用

```csharp
var storageRouter = new StorageRouter(new[]
{
    // HTTP/HTTPSキーを特定のストレージにルーティング
    new StorageRouter.Branch(
        key => key.StartsWith("http://") || key.StartsWith("https://"),
        new FileStorage("HttpStorage")),
    
    // file://キーをプレフィックス除去してルーティング
    new StorageRouter.Branch(
        key => key.StartsWith("file://"),
        key => key.Substring("file://".Length), // キーフォーマッター
        new FileStorage("LocalStorage"))
},
new FileStorage("DefaultStorage")); // デフォルトストレージ

// キーに基づいて適切なストレージに書き込み
await storageRouter.WriteAsync("http://example.com/data.txt", data);
await storageRouter.WriteAsync("file://local/data.txt", data); // LocalStorageに"local/data.txt"として書き込み
await storageRouter.WriteAsync("regular-file.txt", data); // DefaultStorageに書き込み
```

### 追加のドキュメント

- [overview](./Documents/overview.md)

## 注意事項

- 一時ファイルは自動的に管理されますが、大量のデータを扱う場合は適切なクリーンアップを考慮してください
- キャッシュ機能はメモリ使用量に注意して使用してください
- ZIPパッケージ機能はSharpZipLibライブラリを使用しています

### EncryptedStorageのセキュリティ考慮事項

- **パスワード管理**: 暗号化ストレージのパスワード/キーは適切に管理し、紛失しないよう注意してください
- **キー保存**: 暗号化キーをソースコードにハードコードしないでください
- **アルゴリズム**: AES-256暗号化をCBCモードとPKCS7パディングで使用しています
- **IV管理**: 初期化ベクトル（IV）はインスタンス毎に固定です - 本番環境では適切なキーローテーションを実装してください
- **パフォーマンス**: 暗号化・復号化は計算処理のオーバーヘッドが発生します - 高スループット環境では考慮してください

## 生成AIの利用について

- 本repoはChatGPT, Cursorによる生成コードを含みます。

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。 
