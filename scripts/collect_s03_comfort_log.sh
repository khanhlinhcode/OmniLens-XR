#!/bin/zsh

set -euo pipefail

OMNILENS_WORKSPACE=$(cd "$(dirname "$0")/.." && pwd)
OMNILENS_DEVICE_NAME=${OMNILENS_DEVICE_NAME:-iPhone}
OMNILENS_BUNDLE_ID=com.omnilensxr.local.s02.f4c9e27a
OMNILENS_TEMP_DIRECTORY=$(mktemp -d /tmp/omnilens-s03-device-log.XXXXXX)
OMNILENS_RAW_FILE="$OMNILENS_TEMP_DIRECTORY/omnilens-s03-comfort.jsonl"
OMNILENS_COMMAND_LOG="$OMNILENS_TEMP_DIRECTORY/devicectl.log"

cleanup() {
  rm -rf -- "$OMNILENS_TEMP_DIRECTORY"
}
trap cleanup EXIT

if ! xcrun devicectl device copy from \
  --device "$OMNILENS_DEVICE_NAME" \
  --domain-type appDataContainer \
  --domain-identifier "$OMNILENS_BUNDLE_ID" \
  --source Documents/omnilens-s03-comfort.jsonl \
  --destination "$OMNILENS_RAW_FILE" \
  >"$OMNILENS_COMMAND_LOG" 2>&1; then
  "$OMNILENS_WORKSPACE/scripts/sanitize_s02_evidence.py" "$OMNILENS_COMMAND_LOG" >&2
  exit 1
fi

"$OMNILENS_WORKSPACE/scripts/sanitize_s02_evidence.py" "$OMNILENS_RAW_FILE"
