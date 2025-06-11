# Define your app directory
$targetPath = "D:\reps\DigitalProduction\server"

# Find and stop only the node processes running from the target path
Get-WmiObject Win32_Process -Filter "Name = 'node.exe'" | Where-Object {
    $_.CommandLine -like "*$targetPath*server.js*"
} | ForEach-Object {
    Write-Output "Stopping process ID $($_.ProcessId)..."
    Stop-Process -Id $_.ProcessId -Force
}