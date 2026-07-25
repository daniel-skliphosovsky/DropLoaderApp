# Build TikTokExplode as DLL and copy to lib folder
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
New-Item -ItemType Directory -Force -Path lib

dotnet publish "../TikTokExplode/src/TikTokExplode/TikTokExplode.csproj" --configuration Release --output "./lib"

Write-Host "TikTokExplode.dll copied to lib/"
Get-ChildItem lib/TikTokExplode.dll
