#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

scene_path="Assets/Plummet/Scenes/PlummetMVP.unity"

echo "Plummet review checks"
echo "---------------------"

if [[ ! -f "$scene_path" ]]; then
  echo "Missing scene: $scene_path"
  exit 1
fi

if ! grep -q "path: $scene_path" ProjectSettings/EditorBuildSettings.asset; then
  echo "Build Settings does not include $scene_path"
  exit 1
fi

if [[ -f "Assets/Save.unity" || -f "Assets/Save.unity.meta" ]]; then
  echo "Warning: Assets/Save.unity exists. Do not commit it unless it is intentional."
fi

for required in docs/PROMPTS.md docs/BUILD_LOG.md docs/ACCEPTANCE_CHECKLIST.md docs/PLUMMET_SPEC.md; do
  if [[ ! -f "$required" ]]; then
    echo "Missing doc: $required"
    exit 1
  fi
done

if ! grep -R "Plummet/Build Latest Playable" Assets/Plummet/Scripts/Editor >/dev/null; then
  echo "Missing main Unity menu command: Plummet > Build Latest Playable"
  exit 1
fi

echo "Static review checks passed."
echo "Next: run Unity edit-mode tests and the acceptance checklist before pushing gameplay changes."
