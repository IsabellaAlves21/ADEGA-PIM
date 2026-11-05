@echo off
chcp 65001 >nul
cd /d "%~dp0"
where dotnet >nul 2>&1
if errorlevel 1 (
  echo ERRO: Instale o SDK .NET 10 antes de iniciar.
  echo Site oficial: https://dotnet.microsoft.com/download/dotnet/10.0
  pause
  exit /b 1
)
dotnet restore
if errorlevel 1 (
  echo Nao foi possivel baixar as dependencias. Confira a internet.
  pause
  exit /b 1
)
dotnet run
pause
