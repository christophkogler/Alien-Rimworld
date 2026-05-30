#!/usr/bin/env bash

set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/verify-on-linux-build-base.sh WORKTREE_NAME [RIMWORLD_MODS_DIR]

Create a detached temporary worktree from linux-build-base, cherry-pick the
current branch's commits since main, copy local build helpers, then run
scripts/build-and-sync.sh from that worktree.

WORKTREE_NAME may be either a simple name, which is created under /tmp, or an
explicit path.

Examples:
  scripts/verify-on-linux-build-base.sh alien-issue49-verify
  scripts/verify-on-linux-build-base.sh /tmp/alien-issue49-verify "$HOME/.steam/steam/steamapps/common/RimWorld/Mods"
EOF
}

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

if [[ $# -lt 1 || $# -gt 2 ]]; then
  usage >&2
  exit 2
fi

worktree_name="$1"
mods_dir="${2:-}"

if [[ "${worktree_name}" == */* ]]; then
  worktree_path="${worktree_name}"
else
  worktree_path="/tmp/${worktree_name}"
fi

current_branch="$(git -C "${repo_root}" rev-parse --abbrev-ref HEAD)"
if [[ "${current_branch}" == "HEAD" ]]; then
  printf 'Current repository is already in detached HEAD; switch to an issue branch first.\n' >&2
  exit 1
fi

if [[ -e "${worktree_path}" ]]; then
  printf 'Worktree path already exists: %s\n' "${worktree_path}" >&2
  printf 'Remove it first if it is disposable.\n' >&2
  exit 1
fi

tracked_changes="$(
  git -C "${repo_root}" status --porcelain --untracked-files=no |
    grep -v -E '^.. 1\.6/Assemblies/XMT\.dll$' || true
)"
if [[ -n "${tracked_changes}" ]]; then
  printf 'Tracked changes are present in the current worktree.\n' >&2
  printf 'Commit or stash them before verification so only committed source changes are replayed.\n' >&2
  printf '%s\n' "${tracked_changes}" >&2
  exit 1
fi

main_ref="main"
if git -C "${repo_root}" show-ref --verify --quiet refs/remotes/origin/main; then
  main_ref="origin/main"
fi

merge_base="$(git -C "${repo_root}" merge-base "${main_ref}" HEAD)"
mapfile -t commits < <(git -C "${repo_root}" rev-list --reverse "${merge_base}..HEAD")

if [[ "${#commits[@]}" -eq 0 ]]; then
  printf 'No commits found on %s since %s; nothing to verify.\n' "${current_branch}" "${main_ref}" >&2
  exit 1
fi

printf 'Creating detached worktree at %s from linux-build-base...\n' "${worktree_path}"
git -C "${repo_root}" worktree add --detach "${worktree_path}" linux-build-base

printf 'Copying local build props and helper scripts...\n'
if [[ -f "${repo_root}/Directory.Build.local.props" ]]; then
  cp "${repo_root}/Directory.Build.local.props" "${worktree_path}/Directory.Build.local.props"
else
  printf 'Warning: Directory.Build.local.props was not found; build references may not resolve.\n' >&2
fi

rm -rf "${worktree_path}/scripts"
cp -R "${repo_root}/scripts" "${worktree_path}/scripts"

printf 'Cherry-picking %s commit(s) from %s...\n' "${#commits[@]}" "${current_branch}"
for commit in "${commits[@]}"; do
  git -C "${worktree_path}" cherry-pick "${commit}"
done

printf 'Running build-and-sync from %s...\n' "${worktree_path}"
if [[ -n "${mods_dir}" ]]; then
  "${worktree_path}/scripts/build-and-sync.sh" "${mods_dir}"
else
  "${worktree_path}/scripts/build-and-sync.sh"
fi

printf 'Verification worktree remains at %s\n' "${worktree_path}"
