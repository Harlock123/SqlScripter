#!/usr/bin/env bash
#
# Regenerates the screenshots and rebuilds USAGEDOCUMENT_ForSQLSCRIPTER.DOCX.
#
# Steps:
#   1. Render the UI screenshots headlessly (tools/Screenshots).
#   2. Build the DOCX from those screenshots (tools/gen_usage_doc.py) in a
#      local Python venv that has python-docx installed.
#
# Usage:  ./make-usage-doc.sh
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VENV="$SCRIPT_DIR/.docvenv"

echo "==> Rendering screenshots"
dotnet run --project "$SCRIPT_DIR/tools/Screenshots" -c Release -- "$SCRIPT_DIR/screenshots"

if [ ! -x "$VENV/bin/python" ]; then
  echo "==> Creating Python venv for python-docx"
  python3 -m venv "$VENV"
  "$VENV/bin/pip" install --quiet --upgrade pip python-docx
fi

echo "==> Building DOCX"
"$VENV/bin/python" "$SCRIPT_DIR/tools/gen_usage_doc.py"

echo "Done. -> $SCRIPT_DIR/USAGEDOCUMENT_ForSQLSCRIPTER.DOCX"
