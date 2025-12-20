#!/usr/bin/env zsh
set -e

cyan="\033[36m"
green="\033[32m"
yellow="\033[33m"
red="\033[31m"
reset="\033[0m"

echo "${cyan}Building Make Your Choice for Windows…${reset}"
echo ""

# Check if dotnet is installed
if ! command -v dotnet >/dev/null 2>&1; then
  echo "${red}❌ Error: .NET SDK is not installed.${reset}"
  echo "Please install .NET 6.0 SDK or later from:"
  echo "  https://dotnet.microsoft.com/download"
  exit 1
fi

# Check .NET version
dotnetVersion="$(dotnet --version)"
echo "${green}📦 Using .NET SDK version: ${dotnetVersion}${reset}"
echo ""

# Clean previous builds
if [[ -d "bin" || -d "obj" ]]; then
  echo "${yellow}Cleaning previous builds…${reset}"
  rm -rf bin obj
fi

# Build in release mode with single-file publish
echo "${cyan}📦 Building in release mode for win-x64…${reset}"
dotnet publish MakeYourChoice.csproj -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true

status=$?

if [[ $status -eq 0 ]]; then
  echo ""
  echo "${green}✅ Build successful!${reset}"
  echo ""
  echo "${cyan}Binary location:${reset}"
  echo "   ./bin/Release/net6.0-windows/win-x64/publish/MakeYourChoice.exe"
  echo ""
  echo "${yellow}Note: The application requires Administrator privileges to modify the hosts file.${reset}"
else
  echo ""
  echo "${red}❌ Build failed!${reset}"
  exit $status
fi
