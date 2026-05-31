#!/bin/bash
# Chạy toàn bộ Boat Physics Tests trong terminal.
# Dùng: ./run-tests.sh

UNITY="/Applications/Unity/Hub/Editor/6000.4.7f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "$0")" && pwd)"
LOG="/tmp/cho-noi-test-$(date +%s).log"

echo ""
echo "=================================================="
echo "  CHO NOI MIEN TAY — Boat Physics Test Runner"
echo "=================================================="
echo "  Project : $PROJECT"
echo "  Log     : $LOG"
echo ""

# Chạy Unity batch mode
"$UNITY" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT" \
  -executeMethod ChoNoi.Editor.BoatTestRunner.Run \
  -logFile "$LOG" 2>/dev/null

EXIT_CODE=$?

# Lọc và hiện kết quả từ log
echo "--- KET QUA TEST ---"
echo ""
grep -E "\[(PASS|FAIL)\]|^---|\bKET QUA\b|=== " "$LOG" \
  | sed 's/.*\[PASS\]/  ✓  [PASS]/g' \
  | sed 's/.*\[FAIL\]/  ✗  [FAIL]/g'

echo ""

# Tổng kết
TOTAL_PASS=$(grep "\[PASS\]" "$LOG" 2>/dev/null | wc -l | tr -d ' ')
TOTAL_FAIL=$(grep "\[FAIL\]" "$LOG" 2>/dev/null | wc -l | tr -d ' ')
TOTAL_PASS=${TOTAL_PASS:-0}
TOTAL_FAIL=${TOTAL_FAIL:-0}

if [ "$TOTAL_FAIL" -eq 0 ]; then
  echo "  RESULT: ALL $TOTAL_PASS TESTS PASSED"
else
  echo "  RESULT: $TOTAL_FAIL FAILED / $((TOTAL_PASS + TOTAL_FAIL)) TOTAL"
  echo ""
  echo "  Xem log day du: cat $LOG | grep -E '(PASS|FAIL|error)'"
fi

echo ""
echo "=================================================="
exit $EXIT_CODE
