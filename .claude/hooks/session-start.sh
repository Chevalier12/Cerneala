#!/bin/bash
# SessionStart hook for Claude Code cloud sessions (Linux containers).
# Installs the .NET SDK from global.json, the .NET 8 runtime used by most test projects, dotnet-csi, PowerShell,
# and the tools that let real Cerneala windows render without a screen or GPU: Xvfb (virtual X display),
# Lavapipe (mesa-vulkan-drivers, CPU Vulkan for SDL GPU's SPIR-V path), ffmpeg (reproduction GIFs) and Wine
# (runs the published win-x64 reproduction .exe files).
# Local Windows sessions are skipped. Every step checks first, so a cached container returns quickly.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

repo="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "$0")/../.." && pwd)}"
dotnet_dir="$HOME/.dotnet"

export DOTNET_ROOT="$dotnet_dir"
export PATH="$dotnet_dir:$dotnet_dir/tools:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

apt_packages=(mesa-vulkan-drivers xvfb ffmpeg wine wine64 libwine fonts-wine)
missing_packages=()
for package in "${apt_packages[@]}"; do
  if ! dpkg -s "$package" >/dev/null 2>&1; then
    missing_packages+=("$package")
  fi
done
if [ "${#missing_packages[@]}" -gt 0 ]; then
  apt-get update -qq
  DEBIAN_FRONTEND=noninteractive apt-get install -y -qq --no-install-recommends "${missing_packages[@]}"
fi

sdk_version="$(sed -nE 's/.*"version"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/p' "$repo/global.json" | head -n 1)"
needs_sdk=true
if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks | grep -q "^$sdk_version "; then
  needs_sdk=false
fi
needs_runtime8=true
if command -v dotnet >/dev/null 2>&1 && dotnet --list-runtimes | grep -q '^Microsoft\.NETCore\.App 8\.'; then
  needs_runtime8=false
fi
if [ "$needs_sdk" = true ] || [ "$needs_runtime8" = true ]; then
  installer_dir="$(mktemp -d)"
  trap 'rm -rf "$installer_dir"' EXIT
  curl -sSL -o "$installer_dir/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh
  if [ "$needs_sdk" = true ]; then
    bash "$installer_dir/dotnet-install.sh" --jsonfile "$repo/global.json" --install-dir "$dotnet_dir"
  fi
  if [ "$needs_runtime8" = true ]; then
    bash "$installer_dir/dotnet-install.sh" --channel 8.0 --runtime dotnet --install-dir "$dotnet_dir"
  fi
fi

installed_tools="$(dotnet tool list -g)"
if ! grep -qi '^dotnet-csi ' <<<"$installed_tools"; then
  dotnet tool install -g dotnet-csi
fi
if ! grep -qi '^powershell ' <<<"$installed_tools"; then
  dotnet tool install -g PowerShell
fi

# EnableWindowsTargeting: MSBuild reads environment variables as properties, so net8.0-windows projects build on
# Linux without passing -p:EnableWindowsTargeting=true to every command.
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo "export DOTNET_ROOT=\"$dotnet_dir\""
    echo "export PATH=\"$dotnet_dir:$dotnet_dir/tools:\$PATH\""
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
    echo 'export DOTNET_NOLOGO=1'
    echo 'export EnableWindowsTargeting=true'
  } >>"$CLAUDE_ENV_FILE"
fi
