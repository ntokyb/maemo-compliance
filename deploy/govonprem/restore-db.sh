#!/bin/bash
#
# PostgreSQL Database Restore Script for Maemo GovOnPrem
# 
# This script restores a Maemo database from a backup file.
# 
# WARNING: This will overwrite the existing database!
# 
# Usage:
#   restore-db.sh <backup_file>
# 
# Environment Variables Required:
#   DB_HOST - PostgreSQL host (default: localhost)
#   DB_NAME - Database name (default: maemo_gov)
#   DB_USER - Database user (default: maemo_user)
#   DB_PASSWORD - Database password (required)
#
# Optional Environment Variables:
#   DB_PORT - PostgreSQL port (default: 5432)
#   DROP_EXISTING - Set to "true" to drop existing database before restore (default: false)
#

set -euo pipefail

# Default values
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-maemo_gov}"
DB_USER="${DB_USER:-maemo_user}"
DROP_EXISTING="${DROP_EXISTING:-false}"

# Validate required environment variables
if [ -z "${DB_PASSWORD:-}" ]; then
    echo "ERROR: DB_PASSWORD environment variable is required" >&2
    exit 1
fi

# Validate backup file argument
if [ $# -lt 1 ]; then
    echo "ERROR: Backup file path is required" >&2
    echo "Usage: $0 <backup_file>" >&2
    exit 1
fi

BACKUP_FILE="$1"

# Check if backup file exists
if [ ! -f "$BACKUP_FILE" ]; then
    echo "ERROR: Backup file not found: $BACKUP_FILE" >&2
    exit 1
fi

# Set PGPASSWORD for psql (non-interactive password)
export PGPASSWORD="$DB_PASSWORD"

echo "Starting database restore..."
echo "Host: $DB_HOST:$DB_PORT"
echo "Database: $DB_NAME"
echo "User: $DB_USER"
echo "Backup file: $BACKUP_FILE"

# Confirm restore operation
echo ""
echo "WARNING: This operation will overwrite the existing database!"
read -p "Are you sure you want to continue? (yes/no): " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
    echo "Restore cancelled."
    exit 0
fi

# Determine if backup is compressed
RESTORE_FILE="$BACKUP_FILE"
if [[ "$BACKUP_FILE" == *.gz ]]; then
    echo "Decompressing backup file..."
    RESTORE_FILE="${BACKUP_FILE%.gz}"
    gunzip -c "$BACKUP_FILE" > "$RESTORE_FILE"
    TEMP_FILE=true
else
    TEMP_FILE=false
fi

# Drop existing database if requested
if [ "$DROP_EXISTING" = "true" ]; then
    echo "Dropping existing database..."
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres -c "DROP DATABASE IF EXISTS $DB_NAME;" || true
    echo "Creating new database..."
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres -c "CREATE DATABASE $DB_NAME;"
fi

# Restore database
echo "Restoring database from backup..."
if psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "$RESTORE_FILE"; then
    echo "Database restore completed successfully!"
    
    # Clean up temporary file if we decompressed
    if [ "$TEMP_FILE" = "true" ]; then
        rm -f "$RESTORE_FILE"
    fi
    
    exit 0
else
    echo "ERROR: Restore failed!" >&2
    
    # Clean up temporary file if we decompressed
    if [ "$TEMP_FILE" = "true" ]; then
        rm -f "$RESTORE_FILE"
    fi
    
    exit 1
fi

