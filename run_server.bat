@echo off
cd /d "%~dp0"
call stop_server.bat
start "" "publish\RememberTheMilk.exe"
