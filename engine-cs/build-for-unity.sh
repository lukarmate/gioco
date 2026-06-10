#!/usr/bin/env bash
# Compila Engine.Core e copia la DLL nel progetto Unity (Assets/Plugins/Engine).
# Lancia questo dopo ogni modifica all'engine, poi torna in Unity (ricompila da solo).
set -euo pipefail

export PATH="$HOME/.dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

HERE="$(cd "$(dirname "$0")" && pwd)"
DLL="$HERE/Engine.Core/bin/Release/netstandard2.1/Engine.Core.dll"
PLUG="$HERE/../GiocoTCG/Assets/Plugins/Engine"

echo "→ build Engine.Core (Release)…"
dotnet build "$HERE/Engine.Core/Engine.Core.csproj" -c Release | tail -3

mkdir -p "$PLUG"
cp "$DLL" "$PLUG/Engine.Core.dll"
echo "✓ DLL copiata in: $PLUG/Engine.Core.dll"
echo "  Torna in Unity: ricompila in automatico."
