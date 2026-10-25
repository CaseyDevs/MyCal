#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
docker compose -f compose.local.yml up -d --wait
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
dotnet build MyCal.Tests/MyCal.Tests.csproj --disable-build-servers -m:1 -p:UseSharedCompilation=false
dotnet MyCal.Tests/bin/Debug/net10.0/MyCal.Tests.dll --report-trx --results-directory .local/test-results
