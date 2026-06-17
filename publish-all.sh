#!/usr/bin/env bash
#
# Publishes self-contained, single-file SqlScripter executables for every
# supported platform: Intel/AMD (x64) and ARM64 on Windows, macOS and Linux.
#
# For macOS / Linux. Uses only bash + the .NET SDK (no PowerShell required).
# On Windows, use publish-all.ps1 instead.
#
# Usage:
#   ./publish-all.sh                          # all targets -> ./publish/<rid>/
#   ./publish-all.sh ./dist                   # custom output root
#   ./publish-all.sh ./dist "win-x64,linux-x64"   # subset of runtimes
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/SqlScripter.csproj"

OUT_ROOT="${1:-./publish}"
RIDS="${2:-win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64}"
RIDS="${RIDS//,/ }"   # accept comma- or space-separated lists

for rid in $RIDS; do
  echo "==> Publishing $rid"
  dotnet publish "$PROJECT" -c Release -r "$rid" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -o "$OUT_ROOT/$rid"
done

echo "Done. Single-file executables are under $OUT_ROOT/<rid>/"
