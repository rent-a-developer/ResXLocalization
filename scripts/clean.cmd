@echo off
rem Removes all bin/ and obj/ folders in the repository.
cd /d "%~dp0.." || exit /b 1
for /d /r . %%d in (bin obj) do @if exist "%%d" rd /s /q "%%d"