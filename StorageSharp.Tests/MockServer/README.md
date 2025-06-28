# StorageSharp Mock Server

StorageSharpのE2Eテスト用のPython Mockサーバーです。

## 概要

このMockサーバーは、HTTP APIを通じてストレージ操作を提供します。StorageSharpのE2Eテストで使用され、実際のストレージシステムをシミュレートします。

## 機能

- **ファイル一覧取得**: `GET /list`
- **ファイル読み取り**: `GET /read/{key}`
- **ファイル書き込み**: `POST /write/{key}`
- **ファイル削除**: `DELETE /delete/{key}`
- **ヘルスチェック**: `GET /health`

## 必要条件

- Python 3.6以上
- 標準ライブラリのみ使用（追加の依存関係は不要）

## 使用方法

### 手動起動

#### Windows
```cmd
cd StorageSharp.Tests\MockServer
start_server.bat
```

#### Linux/macOS
```bash
cd StorageSharp.Tests/MockServer
chmod +x start_server.sh
./start_server.sh
```

#### 直接実行
```bash
python mock_storage_server.py
# または
python3 mock_storage_server.py
```

### カスタム設定

ホストとポートを指定して起動：

```bash
python mock_storage_server.py 0.0.0.0 9000
```

## API仕様

### レスポンス形式

すべてのAPIはJSON形式でレスポンスを返します。

#### 成功レスポンス例
```json
{
  "status": "success",
  "data": "..."
}
```

#### エラーレスポンス例
```json
{
  "error": "Key 'nonexistent.txt' not found",
  "code": 404
}
```

### エンドポイント詳細

#### GET /health
サーバーのヘルスチェック

**レスポンス:**
```json
{
  "status": "ok",
  "timestamp": 1640995200.0
}
```

#### GET /list
保存されているすべてのキーを取得

**レスポンス:**
```json
{
  "keys": ["file1.txt", "file2.txt", "file3.txt"]
}
```

#### GET /read/{key}
指定されたキーのデータを読み取り

**レスポンス:**
```json
{
  "key": "file1.txt",
  "data": "SGVsbG8gV29ybGQ=",
  "size": 11
}
```

#### POST /write/{key}
指定されたキーにデータを書き込み

**リクエストボディ:**
```json
{
  "data": "SGVsbG8gV29ybGQ="
}
```

**レスポンス:**
```json
{
  "key": "file1.txt",
  "status": "success",
  "size": 11
}
```

#### DELETE /delete/{key}
指定されたキーを削除

**レスポンス:**
```json
{
  "key": "file1.txt",
  "status": "deleted"
}
```

## データ形式

- すべてのデータはBase64エンコードされて送受信されます
- キーはURLエンコードされます
- ファイルサイズに制限はありません（メモリ容量内）

## テストでの使用

E2Eテストでは、`ApiStorageE2ETests`クラスが自動的にこのサーバーを起動し、テスト実行後に停止します。

```csharp
[Fact]
public async Task ApiStorage_BasicOperations()
{
    // サーバーは自動的に起動されます
    var apiStorage = new ApiStorage("http://localhost:8080");
    
    // テスト実行
    await apiStorage.WriteAsync("test.txt", Encoding.UTF8.GetBytes("test data"));
    var data = await apiStorage.ReadAsync("test.txt");
    
    // サーバーは自動的に停止されます
}
```

## トラブルシューティング

### Pythonが見つからない
- Pythonがインストールされていることを確認
- PATHにPythonが含まれていることを確認
- `python --version`または`python3 --version`で確認

### ポートが使用中
- デフォルトポート8080が使用中の場合は、別のポートを指定
- `python mock_storage_server.py localhost 8081`

### サーバーが起動しない
- ファイアウォールの設定を確認
- 他のプロセスがポートを使用していないか確認
- ログメッセージを確認

## 開発

### ログレベル
ログレベルは`logging.basicConfig(level=logging.INFO)`で設定されています。

### スレッドセーフ
MockStorageクラスはスレッドセーフに実装されており、複数のクライアントからの同時アクセスに対応しています。

### 拡張
新しいエンドポイントを追加する場合は、`StorageRequestHandler`クラスにメソッドを追加してください。 