#!/usr/bin/env bash
# Remove only the browser suite's deliberately invalid test email addresses.
set -euo pipefail
cd "$(dirname "$0")/.."
docker compose -f compose.local.yml exec -T postgres psql -U mycal -d mycal -v ON_ERROR_STOP=1 <<'SQL'
DELETE FROM "Users" WHERE "Email" LIKE 'mycal-e2e-%@example.invalid';
SQL
docker compose -f compose.local.yml exec -T postgres psql -U mycal -d identitydb -v ON_ERROR_STOP=1 <<'SQL'
BEGIN;
DELETE FROM "BetaLinks" WHERE "Email" LIKE 'mycal-e2e-%@example.invalid';
DELETE FROM "AspNetUsers" WHERE "Email" LIKE 'mycal-e2e-%@example.invalid';
COMMIT;
SQL
