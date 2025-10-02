@echo off
echo ====================================
echo Building LeadSearchParser...
echo ====================================
echo.

dotnet restore LeadSearchParser.sln
if %errorlevel% neq 0 (
    echo Failed to restore packages!
    pause
    exit /b %errorlevel%
)

dotnet build LeadSearchParser.sln --configuration Release
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)

echo.
echo ====================================
echo Build completed successfully!
echo ====================================
echo.
echo Executable location: LeadSearchParser\bin\Release\net8.0\LeadSearchParser.exe
echo.
pause


