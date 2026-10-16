#!/usr/bin/env bash
# No installs, model download, network, service management or permission changes.
set -euo pipefail
fail() { printf '%s\n' "Worker preflight failed: $1" >&2; exit 1; }
[[ $(uname -s) == Linux ]] || fail 'Linux required'
[[ -n ${MODEL_PATH:-} && -n ${MODEL_LICENSE_PATH:-} && -n ${LLAMA_SERVER_VERSION:-} ]] || fail 'missing local artifact pins'
[[ ${MODEL_SHA256:-} =~ ^[[:xdigit:]]{64}$ && ${LLAMA_SERVER_SHA256:-} =~ ^[[:xdigit:]]{64}$ ]] || fail 'invalid SHA256 pins'
binary=/opt/rucmu-ai/gpu/bin/llama-server
model=$(realpath -e -- "$MODEL_PATH")
license=$(realpath -e -- "$MODEL_LICENSE_PATH")
[[ $model == /opt/rucmu-ai/models/*.gguf && $license == /opt/rucmu-ai/models/* ]] || fail 'model/license outside fixed read-only tree'
[[ -f $model && -s $license && -x $binary && ! -w $model && ! -w $binary ]] || fail 'artifacts missing or worker-writable'
[[ $(od -An -tx1 -N4 -- "$model" | tr -d ' \n') == 47475546 ]] || fail 'not a GGUF file'
actual=$(sha256sum -- "$binary"); actual=${actual%% *}
[[ ${actual,,} == ${LLAMA_SERVER_SHA256,,} ]] || fail 'binary SHA256 mismatch'
actual=$(sha256sum -- "$model"); actual=${actual%% *}
[[ ${actual,,} == ${MODEL_SHA256,,} ]] || fail 'model SHA256 mismatch'
version=$(timeout 10 "$binary" --version 2>&1)
version=${version%%$'\n'*}
[[ $version == "$LLAMA_SERVER_VERSION" ]] || fail 'binary version mismatch'
help=$(timeout 10 "$binary" --help 2>&1)
for option in --device --fit --flash-attn --parallel --host --offline --no-webui --no-agent --log-disable --no-kv-offload --poll-batch --batch-size --ubatch-size --cache-type-k --cache-type-v --cache-ram --ctx-checkpoints; do
    [[ $help == *"$option"* ]] || fail "required option unavailable: $option"
done
[[ $help == *'.sock'* ]] || fail 'UDS host support not advertised by pinned binary'
[[ -c /dev/dxg ]] || fail 'WSL GPU device unavailable'
devices=$(timeout 10 "$binary" --list-devices 2>&1)
[[ $devices == *'CUDA0:'* ]] || fail 'CUDA0 unavailable; no CPU fallback'
# Startup child environment is sanitized by systemd; these names must not be added to worker.env.
while IFS= read -r name; do
    case "$name" in
        LLAMA_ARG_*|HF_TOKEN|LLAMA_API_KEY|LD_PRELOAD|LD_LIBRARY_PATH) fail 'unexpected worker runtime environment override' ;;
    esac
done < <(compgen -e)
