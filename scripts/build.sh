#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

cd "${repo_root}"

dotnet msbuild 1.6/Source/XMT.csproj \
  /p:Configuration=Debug \
  /p:FrameworkPathOverride=/usr/lib/mono/4.8-api
