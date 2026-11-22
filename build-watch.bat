@echo off
rem Watches the project and rebuilds the exe automatically whenever code or assets change.
rem Close the Unity Editor first. Press Ctrl+C to stop.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" -Watch %*
