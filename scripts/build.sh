#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

cd "${repo_root}"

rimworld_install_dir="${RIMWORLD_INSTALL_DIR:-${repo_root}/../../RimWorldLinux_Data}"
workshop_content_dir="${RIMWORLD_WORKSHOP_CONTENT_DIR:-${repo_root}/../../../../workshop/content/294100}"

if [[ ! -f "${rimworld_install_dir}/Managed/Assembly-CSharp.dll" ]]; then
  printf 'RimWorld managed assembly not found: %s\n' "${rimworld_install_dir}/Managed/Assembly-CSharp.dll" >&2
  printf 'Set RIMWORLD_INSTALL_DIR to your RimWorld *_Data directory.\n' >&2
  exit 1
fi

if [[ ! -d "${workshop_content_dir}" ]]; then
  printf 'Steam workshop content directory not found: %s\n' "${workshop_content_dir}" >&2
  printf 'Set RIMWORLD_WORKSHOP_CONTENT_DIR to your workshop/content/294100 directory.\n' >&2
  exit 1
fi

dotnet msbuild 1.6/Source/XMT.csproj \
  /p:Configuration=Debug \
  /p:FrameworkPathOverride=/usr/lib/mono/4.8-api \
  "/p:RimWorldInstallDir=${rimworld_install_dir%/}/" \
  "/p:WorkshopContentDir=${workshop_content_dir%/}/"
