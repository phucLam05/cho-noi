#!/bin/bash
# build-check-phase03.sh — Kiem tra COMPILE + chay UNIT TEST cho Phase 3 (Environment & Tide).
# Dung: ./build-check-phase03.sh
#
# - Mo project trong batchmode, goi ChoNoi.Editor.BoatTestRunner.Run (compile toan bo script).
# - Co loi compile (error CS...) -> in ra va exit 1.
# - Compile sach -> in ket qua [PASS]/[FAIL] (gom nhom 5: Environment & Tide) + tong ket.

UNITY="/Applications/Unity/Hub/Editor/6000.4.7f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "$0")" && pwd)"
LOG="/tmp/cho-noi-build-p3-$(date +%s).log"

echo ""
echo "=================================================="
echo "  CHO NOI MIEN TAY — Build & Test Check (Phase 3)"
echo "=================================================="
echo "  Project : $PROJECT"
echo "  Log     : $LOG"
echo ""

if [ ! -x "$UNITY" ]; then
  echo "  [LOI] Khong tim thay Unity tai: $UNITY"
  echo "        Sua bien UNITY trong build-check-phase03.sh cho dung phien ban."
  exit 1
fi

# Chay Unity batch mode (vua compile vua chay test).
"$UNITY" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT" \
  -executeMethod ChoNoi.Editor.BoatTestRunner.Run \
  -logFile "$LOG" 2>/dev/null

EXIT_CODE=$?

# --- 1. Kiem tra loi COMPILE ---
COMPILE_ERRORS=$(grep -E "error CS[0-9]+" "$LOG" 2>/dev/null | sort -u)

echo "--- 1. KIEM TRA COMPILE ---"
if [ -n "$COMPILE_ERRORS" ]; then
  echo ""
  echo "  ✗  BUILD FAILED — co loi compile:"
  echo ""
  echo "$COMPILE_ERRORS" | sed 's/^/     /'
  echo ""
  echo "=================================================="
  exit 1
fi
echo "  ✓  Khong co loi compile (error CS)."
echo ""

# --- 2. Ket qua UNIT TEST ---
echo "--- 2. KET QUA UNIT TEST ---"
echo ""
grep -E "\[(PASS|FAIL)\]|^---|\bKET QUA\b" "$LOG" \
  | sed 's/.*\[PASS\]/  ✓  [PASS]/g' \
  | sed 's/.*\[FAIL\]/  ✗  [FAIL]/g'
echo ""

TOTAL_PASS=$(grep -c "\[PASS\]" "$LOG" 2>/dev/null | tr -d ' ')
TOTAL_FAIL=$(grep -c "\[FAIL\]" "$LOG" 2>/dev/null | tr -d ' ')
TOTAL_PASS=${TOTAL_PASS:-0}
TOTAL_FAIL=${TOTAL_FAIL:-0}

if [ "$TOTAL_FAIL" -eq 0 ]; then
  echo "  RESULT: BUILD OK — ALL $TOTAL_PASS TESTS PASSED"
else
  echo "  RESULT: $TOTAL_FAIL FAILED / $((TOTAL_PASS + TOTAL_FAIL)) TOTAL"
  echo "  Xem log day du: cat $LOG"
fi

echo ""
echo "=================================================="
exit $EXIT_CODE
