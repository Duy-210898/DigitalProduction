Set WshShell = CreateObject("WScript.Shell")
WshShell.Run "cmd /c for /f ""tokens=5"" %a in ('netstat -ano ^| find "":8000"" ^| find ""LISTENING""') do taskkill /PID %a /F", 0, True
