@echo off
echo ============================================
echo  Environment Builder - REST API Server
echo  Test Brutally - Build Your Level of Complexity
echo ============================================
echo.
echo Starting API server on http://localhost:5000
echo Press Ctrl+C to stop...
echo.
cd /d "%~dp0API"
EnvironmentBuilder.API.exe
pause
