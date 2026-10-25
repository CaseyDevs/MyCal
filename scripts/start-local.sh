#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
docker compose -f compose.local.yml up -d --wait
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
export ASPNETCORE_ENVIRONMENT=Development
dotnet build MyCal.Web/MyCal.Web.csproj --disable-build-servers -m:1 -p:UseSharedCompilation=false
(cd MyCal.Web && dotnet bin/Debug/net10.0/MyCal.Web.dll --migrate)
python3 scripts/create-local-admin.py
printf '\nOpen http://localhost:5099 and use .local/admin-credentials.txt\n\n'
cd MyCal.Web
exec dotnet bin/Debug/net10.0/MyCal.Web.dll --urls http://localhost:5099
