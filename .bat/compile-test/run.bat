@echo off
pushd %~dp0
powershell -ExecutionPolicy Bypass -File .\run.ps1
popd
pause
