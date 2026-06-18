#!/usr/bin/env bash
#
# Renders SqlScripter's UI off-screen (no display, no SQL Server) and writes
# PNG screenshots. Uses the Avalonia headless platform + Skia via the harness
# in tools/Screenshots, seeded with demo objects and a sample script.
#
# Usage:
#   ./screenshots.sh              # -> ./screenshots/*.png
#   ./screenshots.sh ./shots      # custom output directory
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT_DIR="${1:-$SCRIPT_DIR/screenshots}"

dotnet run --project "$SCRIPT_DIR/tools/Screenshots" -c Release -- "$OUT_DIR"
