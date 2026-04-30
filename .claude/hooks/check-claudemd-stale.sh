#!/bin/bash
# Stop hook: if any meaningful project file has been modified more recently
# than CLAUDE.md, re-engage Claude with a reminder to update CLAUDE.md per
# the standing rule. The stop_hook_active guard prevents infinite loops.
#
# Portable: works on any clone of this repo without hard-coded paths.

INPUT=$(cat)
if [ "$(echo "$INPUT" | jq -r '.stop_hook_active // false')" = "true" ]; then
  exit 0
fi

# Resolve project dir: prefer Claude's env var, fall back to script location.
if [ -n "$CLAUDE_PROJECT_DIR" ] && [ -d "$CLAUDE_PROJECT_DIR" ]; then
  PROJECT="$CLAUDE_PROJECT_DIR"
else
  SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  PROJECT="$(cd "$SCRIPT_DIR/../.." && pwd)"
fi

CLAUDE_MD="$PROJECT/CLAUDE.md"
if [ ! -f "$CLAUDE_MD" ]; then
  exit 0
fi

NEWER=$(find "$PROJECT/Assets" "$PROJECT/ProjectSettings" -type f -newer "$CLAUDE_MD" \
  \( -name '*.unity' \
  -o -name '*.cs' \
  -o -name '*.prefab' \
  -o -name '*.mat' \
  -o -name '*.shader' \
  -o -name '*.inputactions' \
  -o -name '*.controller' \
  -o -name '*.asset' \) 2>/dev/null | head -8)

if [ -n "$NEWER" ]; then
  jq -nc --arg n "$NEWER" '{
    decision: "block",
    reason: ("Project files modified more recently than CLAUDE.md:\n" + $n + "\n\nPer the standing instruction (memory: feedback_keep_claudemd_current), review whether CLAUDE.md (Current Status section) needs updating to reflect these changes. If yes, update it now. If the changes are trivial or do not represent meaningful project state, briefly confirm and stop.")
  }'
fi
exit 0
