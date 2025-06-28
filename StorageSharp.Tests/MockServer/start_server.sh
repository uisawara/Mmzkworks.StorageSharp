#!/bin/bash

echo "Starting StorageSharp Mock Server..."
echo

# Pythonパスを検出
if command -v python3 &> /dev/null; then
    PYTHON_CMD=python3
elif command -v python &> /dev/null; then
    PYTHON_CMD=python
else
    echo "Error: Python not found. Please install Python and ensure it's in PATH."
    exit 1
fi

echo "Using Python: $PYTHON_CMD"
echo "Server will start on http://localhost:8080"
echo "Press Ctrl+C to stop the server"
echo

# スクリプトのディレクトリに移動
cd "$(dirname "$0")"

$PYTHON_CMD mock_storage_server.py 