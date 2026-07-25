#!/bin/bash
# Build TikTokExplode as DLL and copy to lib folder
set -e

cd "$(dirname "$0")"
mkdir -p lib

# Build TikTokExplode
dotnet publish ../TikTokExplode/src/TikTokExplode/TikTokExplode.csproj --configuration Release --output ./lib

echo "TikTokExplode.dll copied to lib/"
ls -la lib/TikTokExplode.dll
