@echo off
rem Rebuilds Build\DrownedSanctum.exe if the project changed since the last build.
rem Extra options are passed through, e.g.  build.bat -Force -Regenerate
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
if "%~1"=="" pause
