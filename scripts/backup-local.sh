#!/usr/bin/env bash
set -euo pipefail
umask 077
cd "$(dirname "$0")/.."
backup_dir=".local/backups/$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "$backup_dir"
docker compose -f compose.local.yml exec -T postgres pg_dump -U mycal -Fc mycal > "$backup_dir/mycal.dump"
docker compose -f compose.local.yml exec -T postgres pg_dump -U mycal -Fc identitydb > "$backup_dir/identitydb.dump"
printf 'Backup saved to %s\n' "$backup_dir"
