#!/usr/bin/env bash
# Read-only. Pass the actual RuCMU unit and all three Squad unit names; no guessed names.
set -euo pipefail
[[ $(uname -s) == Linux && $# == 4 ]] || { printf '%s\n' 'Usage: audit-game-placement.sh RUCMU_UNIT SQUAD1_UNIT SQUAD2_UNIT SQUAD3_UNIT' >&2; exit 1; }
[[ -f /sys/fs/cgroup/cgroup.controllers ]] || { printf '%s\n' 'Unified cgroup v2 is required for this template' >&2; exit 1; }
printf '%s\n' "UTC $(date -u +%FT%TZ)"
systemd --version
lscpu -e=CPU,CORE,SOCKET,NODE,ONLINE
for unit in "$@"; do
    [[ $unit =~ ^[a-zA-Z0-9_.@:-]+\.service$ ]] || { printf '%s\n' 'Invalid service unit name' >&2; exit 1; }
    printf '\nUNIT %s\n' "$unit"
    systemctl show -- "$unit" -p LoadState -p ActiveState -p MainPID -p ControlGroup -p CPUAffinity -p AllowedCPUs -p CPUQuotaPerSecUSec -p MemoryCurrent -p MemoryMax
    pid=$(systemctl show --value -p MainPID -- "$unit")
    [[ $pid =~ ^[0-9]+$ && $pid -gt 0 && -r /proc/$pid/status ]] || { printf '%s\n' 'Unit has no running readable main process' >&2; exit 1; }
    # No environment/command line/private game data.
    awk '/^(Name|Pid|Threads|Cpus_allowed_list|Mems_allowed_list):/' "/proc/$pid/status"
    printf '%s\n' 'Thread CPU/NUMA affinity sets (unique):'
    shopt -s nullglob
    statuses=(/proc/"$pid"/task/*/status)
    [[ ${#statuses[@]} -le 4096 ]] || { printf '%s\n' 'Too many tasks for bounded audit' >&2; exit 1; }
    if [[ ${#statuses[@]} -gt 0 ]]; then
        awk '/^(Cpus_allowed_list|Mems_allowed_list):/' "${statuses[@]}" | sort -u
    fi
    group=$(systemctl show --value -p ControlGroup -- "$unit")
    [[ $group == /* && $group != *'..'* ]] || exit 1
    for item in cpuset.cpus.effective cpuset.mems.effective cpu.max cpu.stat cpu.pressure memory.pressure io.pressure; do
        file="/sys/fs/cgroup$group/$item"
        if [[ -r $file ]]; then printf '%s\n' "$file"; cat -- "$file"; else printf 'Unavailable: %s\n' "$item"; fi
    done
done
for resource in cpu memory io; do
    printf '\nGLOBAL PSI %s\n' "$resource"
    cat -- "/proc/pressure/$resource"
done
