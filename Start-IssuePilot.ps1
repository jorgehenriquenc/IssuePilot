param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 5191
)

$ErrorActionPreference = 'Stop'
$secureKey = Read-Host 'Cole sua chave Gemini (a entrada ficará oculta)' -AsSecureString
if ($secureKey.Length -eq 0) {
    throw 'Nenhuma chave foi informada.'
}

$keyPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey)
try {
    $env:GEMINI_API_KEY = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($keyPointer)
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($keyPointer)
    $secureKey.Dispose()
}

$env:ISSUEPILOT_MODEL_PROVIDER = 'gemini'
$env:ISSUEPILOT_GEMINI_MODEL = 'gemini-3.5-flash-lite'
$env:ASPNETCORE_ENVIRONMENT = 'Development'

Push-Location $PSScriptRoot
try {
    Write-Host "IssuePilot: http://127.0.0.1:$Port/"
    Write-Host 'Pressione Ctrl+C para encerrar. A chave será removida desta sessão ao sair.'
    dotnet run --project .\IssuePilot.Web --configuration Release --no-restore --no-launch-profile --urls "http://127.0.0.1:$Port"
}
finally {
    Remove-Item Env:\GEMINI_API_KEY -ErrorAction SilentlyContinue
    Remove-Item Env:\ISSUEPILOT_MODEL_PROVIDER -ErrorAction SilentlyContinue
    Remove-Item Env:\ISSUEPILOT_GEMINI_MODEL -ErrorAction SilentlyContinue
    Pop-Location
}
