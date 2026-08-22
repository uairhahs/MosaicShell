#!/usr/bin/env bash
# Idempotent bootstrap for the MosaicShell development environment.
# Sets up the uv-managed Python 3.14 package plus the Trunk linter used by CI.
set -euo pipefail

export PATH="$HOME/.local/bin:$PATH"

# uv manages the Python 3.14 toolchain and project dependencies (see pyproject.toml / uv.lock).
if ! command -v uv >/dev/null 2>&1; then
  curl -LsSf https://astral.sh/uv/install.sh | sh
fi

# Installs the pinned Python interpreter, creates .venv, and syncs dependencies from uv.lock.
uv sync

# Trunk is the repository's meta-linter (.trunk/trunk.yaml). Install the CLI and pre-fetch its
# hermetic runtimes so `trunk check` is ready without a first-run download. Non-fatal: linting is
# auxiliary and network hiccups fetching linter runtimes must not break dependency setup.
if ! command -v trunk >/dev/null 2>&1; then
  curl -fsSL https://trunk.io/releases/trunk -o "$HOME/.local/bin/trunk" && chmod +x "$HOME/.local/bin/trunk"
fi
trunk install || echo "warning: 'trunk install' did not complete; run it again later if you need linting"

echo "MosaicShell environment ready."
