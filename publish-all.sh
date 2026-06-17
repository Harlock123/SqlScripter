#!/usr/bin/env bash
# Publishes self-contained, single-file builds of SqlScripter for every supported
# target: Intel & ARM on Windows, macOS and Linux. Run from the project root.
set -euo pipefail

RIDS=(win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64)
OUT_ROOT="${1:-./publish}"

for rid in "${RIDS[@]}"; do
  echo "==> Publishing $rid"
  dotnet publish -c Release -r "$rid" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUT_ROOT/$rid"
done

echo "Done. Artifacts are under $OUT_ROOT/<rid>/"
