@echo off
cd /d "D:\reps\DigitalProduction\server"
start "" /b node server.js > server-log.txt 2>&1

schtasks /create /tn "CuttingProjectServerBackground"  /tr "wscript.exe \"D:\Sharedata\CuttingProjectServer\server\launch-server.vbs\""  /sc once /st 07:00   /ru SYSTEM /rl HIGHEST