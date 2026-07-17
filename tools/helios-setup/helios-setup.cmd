@echo off
setlocal
py "%~dp0helios_setup.py" %*
exit /b %ERRORLEVEL%
