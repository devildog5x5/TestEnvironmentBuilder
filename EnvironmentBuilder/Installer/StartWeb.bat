@echo off
echo ============================================
echo  Environment Builder - Web Dashboard
echo  Test Brutally - Build Your Level of Complexity
echo ============================================
echo.
echo Starting Web Dashboard on http://localhost:5001
echo Press Ctrl+C to stop...
echo.
cd /d "%~dp0Web"
EnvironmentBuilder.Web.exe
pause
