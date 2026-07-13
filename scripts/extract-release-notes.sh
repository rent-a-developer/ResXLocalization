#!/usr/bin/env sh
# Validates the CHANGELOG entry for a release and writes its section to release-notes.md.
# Shared by the CI publish and GitHub-release jobs (one source of truth). Takes the version (for
# example 1.0.0) as $1 and requires exactly one dated `## [x.y.z] - YYYY-MM-DD` heading, with no TBD
# and a non-empty section; on failure it emits the ::error annotation and exits non-zero.
set -eu
cd "$(dirname "$0")/.."

version="$1"
escaped_version="$(printf '%s' "${version}" | sed 's/\./\\./g')"

heading_count="$(grep -Ec "^## \[${escaped_version}\] - [0-9]{4}-[0-9]{2}-[0-9]{2}$" CHANGELOG.md || true)"
if [ "${heading_count}" -ne 1 ] || grep -Fq "## [${version}] - TBD" CHANGELOG.md; then
    echo "::error title=Invalid changelog::Expected one dated heading for ${version}, with no TBD."
    exit 1
fi

awk -v ver="${version}" '
    $0 ~ "^## \\[" ver "\\]" { found = 1; next }
    found && /^## \[/ { exit }
    found { print }
' CHANGELOG.md > release-notes.md

if ! grep -q '[^[:space:]]' release-notes.md; then
    echo "::error title=Empty changelog section::Release notes for ${version} are empty."
    exit 1
fi
