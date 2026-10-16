#!/usr/bin/env bash
# Foreground development server only; no installs/downloads/worker management.
set -euo pipefail
root=${RUCMU_AI_TEST_GAME_ROOT:-/home/rucmu/rucmu-ai-test/game}
dotnet=${RUCMU_AI_TEST_DOTNET:-/home/rucmu/rucmu-ai-test/dotnet/dotnet}
[[ $(uname -s) == Linux && $EUID != 0 ]] || { echo 'Use a normal Linux test user.' >&2; exit 1; }
[[ -f $root/bin/Content.Server/Content.Server.dll && -x $dotnet ]] || { echo 'Linux test build missing.' >&2; exit 1; }
[[ -r /proc/pressure/cpu && -r /proc/pressure/memory && -r /proc/pressure/io ]] || { echo 'Linux PSI required; governor stays enabled.' >&2; exit 1; }
echo 'Waiting up to 90 seconds for the manually started local worker.'
deadline=$((SECONDS + 90))
ready=false
while ((SECONDS < deadline)); do
  if [[ -S /run/rucmu-ai/llama.sock && -w /run/rucmu-ai/llama.sock ]] && \
     curl --noproxy '*' --silent --max-time 2 --unix-socket /run/rucmu-ai/llama.sock http://localhost/health | grep -q '"ok"'; then
    ready=true; break
  fi
  sleep 2
done
[[ $ready == true ]] || { echo 'Start worker and check socket group permissions first.' >&2; exit 1; }
export DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p "$root/local-ai-data"
cd "$root"
echo 'LOCAL TEST: port 1213, independent data, auth disabled, hub advertisement disabled.'
echo 'Use listplayers, then promotehost with the actual administrator name (duplicates get a suffix).'
echo 'Private drafts/context are manual; public FAQ and neutral clarification are enabled only for this local test, with no administrators online.'
exec "$dotnet" "$root/bin/Content.Server/Content.Server.dll" \
  --config-file "$root/local-ai-config.toml" --data-dir "$root/local-ai-data" \
  --cvar net.port=1213 --cvar net.bindto=127.0.0.1 --cvar net.upnp=false \
  --cvar auth.mode=2 --cvar hub.advertise=false --cvar status.enabled=false --cvar console.loginlocal=false \
  --cvar config.preset_development=false --cvar config.preset_debug=false \
  --cvar game.hostname=RuCMU-Local-AI-Test --cvar game.lobbyduration=86400 \
  --cvar tts.enabled=false \
  --cvar cmu.admin_ai.enabled=true --cvar cmu.admin_ai.governor_enabled=true \
  --cvar cmu.admin_ai.safe_faq=true \
  --cvar cmu.admin_ai.clarification=true --cvar cmu.admin_ai.investigation=true \
  --cvar cmu.admin_ai.socket_path=/run/rucmu-ai/llama.sock
