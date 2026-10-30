#!/bin/zsh

set -euo pipefail

OMNILENS_WORKSPACE=$(cd "$(dirname "$0")/.." && pwd)
OMNILENS_UNITY_EDITOR=${OMNILENS_UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity}
OMNILENS_UNITY_PROJECT="$OMNILENS_WORKSPACE/client/OmniLensXR"
OMNILENS_COMPILE_LOG=/tmp/omnilens-unity-s04-compile.log
OMNILENS_EDITMODE_LOG=/tmp/omnilens-unity-s04-tests.log
OMNILENS_EDITMODE_RESULTS=/tmp/omnilens-unity-s04-tests.xml
OMNILENS_PLAYMODE_LOG=/tmp/omnilens-unity-s04-playmode.log
OMNILENS_PLAYMODE_RESULTS=/tmp/omnilens-unity-s04-playmode.xml
OMNILENS_BUILD_LOG=/tmp/omnilens-unity-s04-ios-build.log

if [[ ! -x "$OMNILENS_UNITY_EDITOR" ]]; then
  print -u2 -- "Unity Editor not executable: $OMNILENS_UNITY_EDITOR"
  exit 2
fi

"$OMNILENS_UNITY_EDITOR" \
  -batchmode \
  -quit \
  -projectPath "$OMNILENS_UNITY_PROJECT" \
  -executeMethod OmniLensXR.Editor.S04SpatialFixtureBuild.RenderSpatialFixturePreviews \
  -logFile "$OMNILENS_COMPILE_LOG"

"$OMNILENS_UNITY_EDITOR" \
  -batchmode \
  -nographics \
  -projectPath "$OMNILENS_UNITY_PROJECT" \
  -runTests \
  -testPlatform EditMode \
  -testResults "$OMNILENS_EDITMODE_RESULTS" \
  -logFile "$OMNILENS_EDITMODE_LOG"

EDITMODE_RESULT=$(xmllint --xpath 'string(/test-run/@result)' "$OMNILENS_EDITMODE_RESULTS")
EDITMODE_TOTAL=$(xmllint --xpath 'string(/test-run/@total)' "$OMNILENS_EDITMODE_RESULTS")
EDITMODE_PASSED=$(xmllint --xpath 'string(/test-run/@passed)' "$OMNILENS_EDITMODE_RESULTS")
EDITMODE_FAILED=$(xmllint --xpath 'string(/test-run/@failed)' "$OMNILENS_EDITMODE_RESULTS")

if [[ "$EDITMODE_RESULT" != "Passed" || "$EDITMODE_FAILED" != "0" ]]; then
  print -u2 -- "Unity S04 EditMode tests failed: $EDITMODE_RESULT (failed=$EDITMODE_FAILED)"
  exit 4
fi

"$OMNILENS_UNITY_EDITOR" \
  -batchmode \
  -nographics \
  -projectPath "$OMNILENS_UNITY_PROJECT" \
  -runTests \
  -testPlatform PlayMode \
  -testResults "$OMNILENS_PLAYMODE_RESULTS" \
  -logFile "$OMNILENS_PLAYMODE_LOG"

PLAYMODE_RESULT=$(xmllint --xpath 'string(/test-run/@result)' "$OMNILENS_PLAYMODE_RESULTS")
PLAYMODE_TOTAL=$(xmllint --xpath 'string(/test-run/@total)' "$OMNILENS_PLAYMODE_RESULTS")
PLAYMODE_PASSED=$(xmllint --xpath 'string(/test-run/@passed)' "$OMNILENS_PLAYMODE_RESULTS")
PLAYMODE_FAILED=$(xmllint --xpath 'string(/test-run/@failed)' "$OMNILENS_PLAYMODE_RESULTS")

if [[ "$PLAYMODE_RESULT" != "Passed" || "$PLAYMODE_FAILED" != "0" ]]; then
  print -u2 -- "Unity S04 PlayMode tests failed: $PLAYMODE_RESULT (failed=$PLAYMODE_FAILED)"
  exit 5
fi

"$OMNILENS_UNITY_EDITOR" \
  -batchmode \
  -quit \
  -nographics \
  -projectPath "$OMNILENS_UNITY_PROJECT" \
  -executeMethod OmniLensXR.Editor.S04SpatialFixtureBuild.BuildSpatialFixtures \
  -logFile "$OMNILENS_BUILD_LOG"

print -r -- "Unity S04 configure + 3 preview renders: PASS"
print -r -- "Unity S04 EditMode tests: $EDITMODE_RESULT (total=$EDITMODE_TOTAL passed=$EDITMODE_PASSED failed=$EDITMODE_FAILED)"
print -r -- "Unity S04 PlayMode tests: $PLAYMODE_RESULT (total=$PLAYMODE_TOTAL passed=$PLAYMODE_PASSED failed=$PLAYMODE_FAILED)"
print -r -- "Unity S04 spatial-fixture iOS project generation: PASS"
print -r -- "Compile log: $OMNILENS_COMPILE_LOG"
print -r -- "EditMode log: $OMNILENS_EDITMODE_LOG"
print -r -- "EditMode results: $OMNILENS_EDITMODE_RESULTS"
print -r -- "PlayMode log: $OMNILENS_PLAYMODE_LOG"
print -r -- "PlayMode results: $OMNILENS_PLAYMODE_RESULTS"
print -r -- "Build log: $OMNILENS_BUILD_LOG"
