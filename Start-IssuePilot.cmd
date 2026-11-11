@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -Command "$ErrorActionPreference='Stop'; $secureKey=Read-Host 'Cole sua chave Gemini (entrada oculta)' -AsSecureString; if($secureKey.Length -eq 0){throw 'Chave vazia'}; $keyPointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey); try {$env:GEMINI_API_KEY=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($keyPointer)} finally {[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($keyPointer); $secureKey.Dispose()}; $env:ISSUEPILOT_MODEL_PROVIDER='gemini'; $env:ISSUEPILOT_GEMINI_MODEL='gemini-3.5-flash-lite'; Write-Host 'Abra http://127.0.0.1:5191/'; dotnet run --project .\IssuePilot.Web --configuration Release --no-restore --no-launch-profile --urls http://127.0.0.1:5191"
if errorlevel 1 pause
