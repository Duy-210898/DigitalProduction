Set WshShell = CreateObject("WScript.Shell")

' Kill the process using port 8000
WshShell.Run "cmd /c for /f ""tokens=5"" %a in ('netstat -ano ^| find "":8000"" ^| find ""LISTENING""') do taskkill /PID %a /F", 0, True

' Wait a moment
WScript.Sleep 1000

' Restart the server
WshShell.Run """D:\reps\DigitalProduction\server\start-server.bat""", 0, False
