@echo off
echo Starting StorageSharp Mock Server...
echo.

REM Pythonパスを検出
python --version >nul 2>&1
if %errorlevel% == 0 (
    set PYTHON_CMD=python
) else (
    python3 --version >nul 2>&1
    if %errorlevel% == 0 (
        set PYTHON_CMD=python3
    ) else (
        echo Error: Python not found. Please install Python and ensure it's in PATH.
        pause
        exit /b 1
    )
)

echo Using Python: %PYTHON_CMD%
echo Server will start on http://localhost:8080
echo Press Ctrl+C to stop the server
echo.

%PYTHON_CMD% "%~dp0mock_storage_server.py"

pause 