#!/usr/bin/env bash
set -eu
umask 077
if [ "$(uname -s)" != 'Linux' ]; then
    echo 'This acceptance smoke script requires Linux.' >&2
    exit 2
fi

# Runs previously built portable artifacts. No package/model downloads, game services or root privileges.
root=$(CDPATH='' cd -- "$(dirname -- "$0")/../.." && pwd)
spike="$root/Tools/AdminAI.Spike/bin/Debug/net10.0/AdminAI.Spike.dll"
tests="$root/Tools/AdminAI.Tests/bin/Debug/net10.0/AdminAI.Tests.dll"
server_tests="$root/Tools/AdminAI.ServerTests/bin/Debug/net10.0/Content.IntegrationTests.dll"
if ! command -v dotnet >/dev/null || [ ! -f "$spike" ] || [ ! -f "$tests" ]; then
    echo 'A .NET 10 runtime and built AdminAI.Spike/AdminAI.Tests output directories are required.' >&2
    exit 2
fi

cd -- "$root"
reports="$root/.codex-build/admin-ai/linux-smoke-$(date -u +%Y%m%d-%H%M%S)"
mkdir -p -- "$reports"
uname -a >"$reports/environment.log"
dotnet --list-runtimes >>"$reports/environment.log"
socket_dir=$(mktemp -d /tmp/rucmu-ai-smoke-XXXXXX)
socket="$socket_dir/llama.sock"
worker_pid=''
failed=0

stop_worker() {
    if [ -n "$worker_pid" ]; then
        kill "$worker_pid" 2>/dev/null || true
        wait "$worker_pid" 2>/dev/null || true
        worker_pid=''
    fi
    # Only this script's private, newly created test endpoint.
    rm -f -- "$socket"
}
cleanup() {
    stop_worker
    rmdir -- "$socket_dir" 2>/dev/null || true
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

run_check() {
    name=$1
    expected=$2
    shift 2
    if "$@" >"$reports/$name.log" 2>&1; then code=0; else code=$?; fi
    printf '%s\t%s\t%s\n' "$name" "$code" "$expected" >>"$reports/exit-codes.tsv"
    cat -- "$reports/$name.log"
    if [ "$code" -ne "$expected" ]; then failed=1; fi
}

start_worker() {
    cycle=$1
    dotnet "$spike" worker "$socket" >"$reports/worker-$cycle.log" 2>&1 &
    worker_pid=$!
    attempt=0
    while [ "$attempt" -lt 50 ]; do
        if grep -q 'MOCK WORKER ready' "$reports/worker-$cycle.log"; then return 0; fi
        if ! kill -0 "$worker_pid" 2>/dev/null; then break; fi
        attempt=$((attempt + 1))
        sleep 0.1
    done
    cat -- "$reports/worker-$cycle.log" >&2
    echo 'Test worker did not start within five seconds.' >&2
    exit 1
}

export RUCMU_AI_TEST_SOCKET="$socket"
export RUCMU_AI_ENGINE_RESOURCES="$root/RobustToolbox/Resources"
run_check selftest 0 dotnet "$spike" selftest "$socket"
run_check boundary 0 dotnet "$tests" boundary
run_check ipc-faults 0 dotnet "$tests" ipc
start_worker 1
run_check separate-process 0 dotnet "$spike" probe "$socket"
stop_worker
run_check worker-unavailable 2 dotnet "$spike" probe "$socket"
start_worker 2
run_check worker-restarted 0 dotnet "$spike" probe "$socket"
stop_worker

if [ -f "$server_tests" ]; then
    run_check server-thread 0 dotnet "$server_tests" ipc
else
    echo 'Server-thread integration artifact missing; this part of stage A is NOT verified.' >&2
    failed=1
fi

printf 'Reports: %s\n' "$reports"
exit "$failed"
