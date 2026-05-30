#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"
mods_dir="${1:-${HOME}/snap/steam/common/.local/share/Steam/steamapps/common/RimWorld/Mods}"

cd "${repo_root}"

dotnet msbuild 1.6/Source/XMT.csproj \
  /p:Configuration=Debug \
  /p:FrameworkPathOverride=/usr/lib/mono/4.8-api

"${script_dir}/rsync-to-rimworld-mods.sh" "${mods_dir}"
