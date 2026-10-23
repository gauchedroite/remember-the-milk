@echo off
powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='RememberTheMilk.exe'\" | ForEach-Object { Stop-Process -Id $_.ProcessId -Force; Write-Output ('Stopped RememberTheMilk (pid ' + $_.ProcessId + ').') }"
