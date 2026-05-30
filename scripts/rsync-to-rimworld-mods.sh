#!/usr/bin/env bash

set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/rsync-to-rimworld-mods.sh RIMWORLD_MODS_DIR [MOD_NAME]

Deploy the mod files into an explicit RimWorld Mods directory.

Examples:
  scripts/rsync-to-rimworld-mods.sh "$HOME/.steam/steam/steamapps/common/RimWorld/Mods"
  scripts/rsync-to-rimworld-mods.sh /tmp/release-root/Mods Alien-Rimworld

When no destination is provided, this script does not sync files. It checks
common Steam locations for RimWorld Mods directories and prints suggestions.
EOF
}

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

mod_name="${2:-Alien-Rimworld}"

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

if [[ $# -eq 0 ]]; then
  usage
  printf '\nNo destination provided; no files were synced.\n' >&2
  printf '\nLikely RimWorld Mods directories:\n' >&2
  candidate_roots=(
    "${HOME}/.steam/steam"
    "${HOME}/.local/share/Steam"
    "${HOME}/snap/steam/common/.local/share/Steam"
    "${HOME}/Library/Application Support/Steam"
  )
  found=0
  for candidate_root in "${candidate_roots[@]}"; do
    candidate_mods_dir="${candidate_root}/steamapps/common/RimWorld/Mods"
    if [[ -d "${candidate_mods_dir}" ]]; then
      printf '  %s\n' "${candidate_mods_dir}" >&2
      found=1
    fi
  done
  if [[ "${found}" -eq 0 ]]; then
    printf '  No common Steam RimWorld Mods directories found.\n' >&2
  fi
  printf '\nRun again with one of those Mods directories as the first argument.\n' >&2
  exit 2
fi

mods_dir="${1%/}"
destination="${mods_dir}/${mod_name}"

mkdir -p "$destination"

rsync -av --delete \
  --exclude='/1.6/Source/***' \
  --exclude='/Compatibility/*/Source/***' \
  --exclude='/Directory.Build*.props' \
  --exclude='*.csproj' \
  --exclude='*.sln' \
  --exclude='*.user' \
  --exclude='*.pdb' \
  --exclude='*.mdb' \
  --exclude='*.cache' \
  --exclude='*.kra' \
  --exclude='*.kra-*' \
  --exclude='*.kra~' \
  --exclude='*.png~' \
  --exclude='obj/***' \
  --exclude='bin/***' \
  --exclude='.git/***' \
  --exclude='.agents/***' \
  --exclude='.codex/***' \
  --exclude='scripts/***' \
  --include='/About/***' \
  --include='/LoadFolders.xml' \
  --include='/Common/***' \
  --include='/1.6/' \
  --include='/1.6/***' \
  --include='/Compatibility/' \
  --include='/Compatibility/*/' \
  --include='/Compatibility/***' \
  --exclude='*' \
  "${repo_root}/" \
  "${destination}/"

printf 'Synced mod files to %s\n' "$destination"
