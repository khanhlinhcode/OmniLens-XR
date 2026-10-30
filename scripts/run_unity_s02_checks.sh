#!/bin/zsh

set -euo pipefail

OMNILENS_WORKSPACE=$(cd "$(dirname "$0")/.." && pwd)
OMNILENS_UNITY_EDITOR=${OMNILENS_UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity}
OMNILENS_UNITY_PROJECT="$OMNILENS_WORKSPACE/client/OmniLensXR"
OMNILENS_COMPILE_LOG=/tmp/omnilens-unity-s02-compile.log
OMNILENS_TEST_LOG=/tmp/omnilens-unity-s02-tests.log
OMNILENS_TEST_RESULTS=/tmp/omnilens-unity-s02-tests.xml
OMNILENS_BUILD_LOG=/tmp/omnilens-unity-s02-ios-build.log

if [[ ! -x "$OMNILENS_UNITY_EDITOR" ]]; then
  print -u2 -- "Unity Editor not executable: $OMNILENS_UNITY_EDITOR"
  exit 2
fi

"$OMNILENS_UNITY_EDITOR" \
  -batchmode \
  -quit \
  -nographics \
  -projectPath "$OMNILENS_UNITY_PROJECT" \
  -executeMethod OmniLensXR.Editor.S02IosBuild.ImportAndConfigureHelloCardboard \
  -logFile "$OMNILENS_COMPILE_LOG"

"$OMNILENS_UNITY_EDITOR" \
  -batchmode \
  -nographics \
  -projectPath "$OMNILENS_UNITY_PROJECT" \
  -runTests \
  -testPlatform EditMode \
  -testResults "$OMNILENS_TEST_RESULTS" \
  -logFile "$OMNILENS_TEST_LOG"

TEST_RESULT=$(xmllint --xpath 'string(/test-run/@result)' "$OMNILENS_TEST_RESULTS")
TEST_TOTAL=$(xmllint --xpath 'string(/test-run/@total)' "$OMNILENS_TEST_RESULTS")
TEST_PASSED=$(xmllint --xpath 'string(/test-run/@passed)' "$OMNILENS_TEST_RESULTS")
TEST_FAILED=$(xmllint --xpath 'string(/test-run/@failed)' "$OMNILENS_TEST_RESULTS")

if [[ "$TEST_RESULT" != "Passed" || "$TEST_FAILED" != "0" ]]; then
  print -u2 -- "Unity S02 EditMode tests failed: $TEST_RESULT (failed=$TEST_FAILED)"
  exit 4
fi

"$OMNILENS_UNITY_EDITOR" \
  -batchmode \
  -quit \
  -nographics \
  -projectPath "$OMNILENS_UNITY_PROJECT" \
  -executeMethod OmniLensXR.Editor.S02IosBuild.BuildHelloCardboard \
  -logFile "$OMNILENS_BUILD_LOG"

print -r -- "Unity S02 configure/import: PASS"
print -r -- "Unity S02 EditMode tests: $TEST_RESULT (total=$TEST_TOTAL passed=$TEST_PASSED failed=$TEST_FAILED)"
print -r -- "Unity HelloCardboard iOS project generation: PASS"
print -r -- "Compile log: $OMNILENS_COMPILE_LOG"
print -r -- "Test log: $OMNILENS_TEST_LOG"
print -r -- "Test results: $OMNILENS_TEST_RESULTS"
print -r -- "Build log: $OMNILENS_BUILD_LOG"
