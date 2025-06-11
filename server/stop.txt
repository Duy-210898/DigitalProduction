@echo off
cd /d D:\reps\DigitalProduction\server

:: Find and kill the Node.js process running in this folder
for /f "tokens=2" %%a in ('netstat -ano ^| findstr :8080') do (
    tasklist /fi "PID eq %%a" | find /i "node.exe" >nul && taskkill /PID %%a /F
)
