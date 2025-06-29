#!/usr/bin/env python3
"""
StorageSharp E2Eテスト用のMockストレージサーバー
HTTP APIを通じてストレージ操作を提供します
"""

import json
import os
import tempfile
import threading
import time
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs
import base64
import logging

# ログ設定
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class MockStorage:
    """インメモリストレージの実装"""
    
    def __init__(self):
        self._storage = {}
        self._lock = threading.Lock()
    
    def list_all(self):
        """すべてのキーを取得"""
        with self._lock:
            return list(self._storage.keys())
    
    def read(self, key):
        """キーに対応するデータを読み取り"""
        with self._lock:
            if key not in self._storage:
                return None
            return self._storage[key]
    
    def write(self, key, data):
        """キーとデータを保存"""
        with self._lock:
            self._storage[key] = data
            return True
    
    def delete(self, key):
        """キーを削除"""
        with self._lock:
            if key in self._storage:
                del self._storage[key]
                return True
            return False

class StorageRequestHandler(BaseHTTPRequestHandler):
    """HTTPリクエストハンドラー"""
    
    def __init__(self, *args, storage=None, **kwargs):
        self.storage = storage
        super().__init__(*args, **kwargs)
    
    def do_GET(self):
        """GETリクエストの処理"""
        try:
            parsed_url = urlparse(self.path)
            path = parsed_url.path
            
            if path == '/health':
                self._handle_health()
            elif path == '/list':
                self._handle_list()
            elif path.startswith('/read/'):
                key = path[6:]  # '/read/' を除去
                self._handle_read(key)
            else:
                self._send_error(404, "Not Found")
                
        except Exception as e:
            logger.error(f"GET request error: {e}")
            self._send_error(500, str(e))
    
    def do_POST(self):
        """POSTリクエストの処理"""
        try:
            parsed_url = urlparse(self.path)
            path = parsed_url.path
            
            if path.startswith('/write/'):
                key = path[7:]  # '/write/' を除去
                self._handle_write(key)
            else:
                self._send_error(404, "Not Found")
                
        except Exception as e:
            logger.error(f"POST request error: {e}")
            self._send_error(500, str(e))
    
    def do_DELETE(self):
        """DELETEリクエストの処理"""
        try:
            parsed_url = urlparse(self.path)
            path = parsed_url.path
            
            if path.startswith('/delete/'):
                key = path[8:]  # '/delete/' を除去
                self._handle_delete(key)
            else:
                self._send_error(404, "Not Found")
                
        except Exception as e:
            logger.error(f"DELETE request error: {e}")
            self._send_error(500, str(e))
    
    def _handle_health(self):
        """ヘルスチェック"""
        self._send_json_response({"status": "ok", "timestamp": time.time()})
    
    def _handle_list(self):
        """ファイル一覧取得"""
        keys = self.storage.list_all()
        self._send_json_response({"keys": keys})
    
    def _handle_read(self, key):
        """ファイル読み取り"""
        # 固定遅延を追加（キャッシュの効果を分かりやすくするため）
        time.sleep(0.5)  # 500msの遅延
        
        data = self.storage.read(key)
        if data is None:
            self._send_error(404, f"Key '{key}' not found")
            return
        
        # Base64エンコードして返す
        encoded_data = base64.b64encode(data).decode('utf-8')
        self._send_json_response({
            "key": key,
            "data": encoded_data,
            "size": len(data)
        })
    
    def _handle_write(self, key):
        """ファイル書き込み"""
        content_length = int(self.headers.get('Content-Length', 0))
        if content_length == 0:
            self._send_error(400, "No data provided")
            return
        
        # リクエストボディを読み取り
        body = self.rfile.read(content_length)
        
        try:
            # JSONとしてパース
            request_data = json.loads(body.decode('utf-8'))
            data_str = request_data.get('data', '')
            
            # Base64デコード
            data = base64.b64decode(data_str)
            
            # ストレージに保存
            success = self.storage.write(key, data)
            
            if success:
                self._send_json_response({
                    "key": key,
                    "status": "success",
                    "size": len(data)
                })
            else:
                self._send_error(500, "Failed to write data")
                
        except Exception as e:
            self._send_error(400, f"Invalid request data: {e}")
    
    def _handle_delete(self, key):
        """ファイル削除"""
        success = self.storage.delete(key)
        
        if success:
            self._send_json_response({
                "key": key,
                "status": "deleted"
            })
        else:
            self._send_error(404, f"Key '{key}' not found")
    
    def _send_json_response(self, data):
        """JSONレスポンスを送信"""
        response = json.dumps(data, ensure_ascii=False)
        self.send_response(200)
        self.send_header('Content-Type', 'application/json; charset=utf-8')
        self.send_header('Content-Length', str(len(response.encode('utf-8'))))
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, DELETE, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        self.end_headers()
        self.wfile.write(response.encode('utf-8'))
    
    def _send_error(self, code, message):
        """エラーレスポンスを送信"""
        error_data = {"error": message, "code": code}
        response = json.dumps(error_data, ensure_ascii=False)
        self.send_response(code)
        self.send_header('Content-Type', 'application/json; charset=utf-8')
        self.send_header('Content-Length', str(len(response.encode('utf-8'))))
        self.send_header('Access-Control-Allow-Origin', '*')
        self.end_headers()
        self.wfile.write(response.encode('utf-8'))
    
    def log_message(self, format, *args):
        """ログメッセージの出力"""
        logger.info(f"{self.address_string()} - {format % args}")

def create_handler_class(storage):
    """ストレージインスタンスを含むハンドラークラスを作成"""
    return type('StorageRequestHandler', (StorageRequestHandler,), {
        '__init__': lambda self, *args, **kwargs: StorageRequestHandler.__init__(self, *args, storage=storage, **kwargs)
    })

def run_server(host='localhost', port=8080):
    """サーバーを起動"""
    storage = MockStorage()
    handler_class = create_handler_class(storage)
    
    server = HTTPServer((host, port), handler_class)
    logger.info(f"Mock storage server starting on http://{host}:{port}")
    
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        logger.info("Server stopping...")
    finally:
        server.server_close()
        logger.info("Server stopped")

if __name__ == '__main__':
    import sys
    
    host = 'localhost'
    port = 8080
    
    if len(sys.argv) > 1:
        host = sys.argv[1]
    if len(sys.argv) > 2:
        port = int(sys.argv[2])
    
    run_server(host, port) 