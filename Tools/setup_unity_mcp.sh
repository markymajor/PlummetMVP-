#!/usr/bin/env bash
# Connect this Unity project to Claude Code via the CoplayDev Unity MCP server.
#
# Safe to re-run. It checks prerequisites and adds the MCP package to
# Packages/manifest.json (with a backup). The final "Configure" step is a
# one-click action inside Unity by design and is printed at the end.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MANIFEST="$REPO_ROOT/Packages/manifest.json"
PKG_NAME="com.coplaydev.unity-mcp"
PKG_URL="https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main"

echo "==> Plummet: Unity MCP setup"
echo "    repo: $REPO_ROOT"

# 1. Prerequisites ----------------------------------------------------------
if ! command -v python3 >/dev/null 2>&1; then
  echo "!! python3 not found. Install Python 3.10+ first: https://www.python.org/downloads/" >&2
  exit 1
fi
echo "==> python3: $(python3 --version)"

if ! command -v uv >/dev/null 2>&1; then
  echo "==> 'uv' not found. Installing via the official installer..."
  curl -LsSf https://astral.sh/uv/install.sh | sh
  export PATH="$HOME/.local/bin:$HOME/.cargo/bin:$PATH"
fi
if command -v uv >/dev/null 2>&1; then
  echo "==> uv: $(uv --version)"
else
  echo "!! uv still not on PATH. Open a NEW terminal (or add ~/.local/bin to PATH) and re-run." >&2
fi

# 2. Add the Unity package to the manifest (idempotent) ---------------------
if [ ! -f "$MANIFEST" ]; then
  echo "!! $MANIFEST not found. Run this from inside the PlummetMVP- project." >&2
  exit 1
fi

python3 - "$MANIFEST" "$PKG_NAME" "$PKG_URL" <<'PY'
import json, sys, shutil
manifest, name, url = sys.argv[1], sys.argv[2], sys.argv[3]
with open(manifest) as f:
    data = json.load(f)
deps = data.setdefault("dependencies", {})
if deps.get(name) == url:
    print("==> manifest already references %s" % name)
    sys.exit(0)
shutil.copyfile(manifest, manifest + ".bak")
deps[name] = url
with open(manifest, "w") as f:
    json.dump(data, f, indent=2)
    f.write("\n")
print("==> added %s to manifest (backup: manifest.json.bak)" % name)
PY

# 3. Remaining one-click step in Unity --------------------------------------
cat <<'NEXT'

==> Done with the scriptable parts. Now, in Unity (project open):
    1. Let Package Manager finish importing "MCP for Unity"
       (Assets > Refresh if Unity was already open).
    2. Window > MCP for Unity > Configure All Detected Clients
       (this writes the Claude Code connection for you).
    3. Restart Claude Code in this folder, then run:  /mcp
       You should see a "unity" server with editor tools.

To undo the manifest change:  mv Packages/manifest.json.bak Packages/manifest.json
NEXT
