@echo off
echo ============================================
echo  Environment Builder - Full Stack
echo  Test Brutally - Build Your Level of Complexity
echo ============================================
echo.
echo Starting API Server (http://localhost:5000)...
start "Environment Builder API" /D "%~dp0API" EnvironmentBuilder.API.exe

timeout /t 3 /nobreak > nul

echo Starting Web Dashboard (http://localhost:5001)...
start "Environment Builder Web" /D "%~dp0Web" EnvironmentBuilder.Web.exe

timeout /t 2 /nobreak > nul

echo.
echo ============================================
echo  Both services are now running!
echo  API: http://localhost:5000
echo  Web: http://localhost:5001
echo ============================================
echo.
echo Opening Web Dashboard in browser...
start http://localhost:5001

echo.
echo Press any key to stop all services...
pause > nul

echo Stopping services...
taskkill /FI "WINDOWTITLE eq Environment Builder API" /F > nul 2>&1
taskkill /FI "WINDOWTITLE eq Environment Builder Web" /F > nul 2>&1
echo Done.
