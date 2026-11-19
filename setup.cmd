@echo off
REM Ejecuta setup.ps1 sin cambiar la politica de ejecucion de PowerShell.
REM Uso: doble clic, o "setup.cmd -ResetDatabase" desde una consola.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup.ps1" %*
