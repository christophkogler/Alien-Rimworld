#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"
mods_dir="${1:-${HOME}/snap/steam/common/.local/share/Steam/steamapps/common/RimWorld/Mods}"

"${script_dir}/build.sh"

"${script_dir}/rsync-to-rimworld-mods.sh" "${mods_dir}"
