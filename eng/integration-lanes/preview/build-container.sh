#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
image="${HELIOS_PREVIEW_IMAGE:-helios-preview:local}"
ca_cert="${HELIOS_BUILD_CA_CERT:-}"

secret_args=()
if [[ -n "$ca_cert" ]]; then
  [[ -f "$ca_cert" ]] || { echo "CA certificate does not exist: $ca_cert" >&2; exit 2; }
  secret_args=(--secret "id=ca_cert,src=$ca_cert")
fi

if command -v docker >/dev/null 2>&1; then
  DOCKER_BUILDKIT=1 docker build "${secret_args[@]}" --tag "$image" "$script_dir"
  test "$(docker inspect --format '{{ index .Config.Labels "org.helios.release-artifact" }}' "$image")" = false
elif command -v buildah >/dev/null 2>&1; then
  buildah bud --isolation=chroot "${secret_args[@]}" --tag "$image" "$script_dir"
  buildah inspect "$image" | python3 -c 'import json, sys; assert json.load(sys.stdin)["OCIv1"]["config"]["Labels"]["org.helios.release-artifact"] == "false"'
else
  echo 'Docker or Buildah is required.' >&2
  exit 127
fi

echo "Built non-release preview image: $image"
