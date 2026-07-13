#!/usr/bin/env sh
# Removes all bin/ and obj/ folders in the repository.
set -eu
cd "$(dirname "$0")/.."
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
