#!/usr/bin/env bash
# Restore into disposable databases, compare row fingerprints, then remove only those databases.
set -euo pipefail
cd "$(dirname "$0")/.."
backup_dir="${1:?Usage: scripts/verify-backup-local.sh .local/backups/TIMESTAMP}"
restore_app="mycal_restore_check_$(date +%s)_${RANDOM}"
restore_identity="identity_restore_check_$(date +%s)_${RANDOM}"
db() { docker compose -f compose.local.yml exec -T postgres "$@"; }
cleanup() { db dropdb -U mycal --force --if-exists "$restore_app"; db dropdb -U mycal --force --if-exists "$restore_identity"; }
trap cleanup EXIT
db createdb -U mycal "$restore_app"
db createdb -U mycal "$restore_identity"
db pg_restore --exit-on-error --no-owner --no-privileges -U mycal -d "$restore_app" < "$backup_dir/mycal.dump"
db pg_restore --exit-on-error --no-owner --no-privileges -U mycal -d "$restore_identity" < "$backup_dir/identitydb.dump"
compare() {
    local original_db="$1" restored_db="$2" table="$3"
    local query="SELECT md5(COALESCE(string_agg(to_jsonb(t)::text, '' ORDER BY to_jsonb(t)::text), '')) FROM \"${table}\" AS t"
    local original_hash restored_hash
    original_hash="$(db psql -U mycal -d "$original_db" -At -c "$query")"
    restored_hash="$(db psql -U mycal -d "$restored_db" -At -c "$query")"
    if [[ "$original_hash" != "$restored_hash" ]]; then printf 'Verification failed for %s\n' "$table" >&2; exit 1; fi
}
for table in Users Food FoodLog FoodLogEntries Targets Weights SavedFoods Reviews __EFMigrationsHistory; do compare mycal "$restore_app" "$table"; done
for table in AspNetUsers AspNetUserPasskeys BetaLinks __EFMigrationsHistory; do compare identitydb "$restore_identity" "$table"; done
printf 'Restore verified: account records and tracking data match. Live databases were not changed.\n'
