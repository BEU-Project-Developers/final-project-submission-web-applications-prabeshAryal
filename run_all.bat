@echo off
REM Run both backend and frontend for Music App Project

REM Start Backend
start "Backend" cmd /k "cd MusicAppBackend && dotnet run"

REM Start Frontend
start "Frontend" cmd /k "cd MusicAppFrontend && dotnet run"

echo Both backend and frontend are starting in separate windows...
pause
