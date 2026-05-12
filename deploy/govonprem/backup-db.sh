#!/bin/bash
#
# PostgreSQL Database Backup Script for Maemo GovOnPrem
# 
# This script creates a timestamped backup of the Maemo database.
# 
# Environment Variables Required:
#   DB_HOST - PostgreSQL host (default: localhost)
#   DB_NAME - Database name (default: maemo_gov)
#   DB_USER - Database user (default: maemo_user)
#   DB_PASSWORD - Database password (required)
#
# Optional Environment Variables:
#   DB_PORT - PostgreSQL port (default: 5432)
#   BACKUP_DIR - Directory to store backups (default: ./backups)
#   BACKUP_RETENTION_DAYS - Number of days to keep backups (default: 30)
#

set -euo pipefail

# Default values
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-maemo_gov}"
DB_USER="${DB_USER:-maemo_user}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"

# Validate required environment variables
if [ -z "${DB_PASSWORD:-}" ]; then
    echo "ERROR: DB_PASSWORD environment variable is required" >&2
    exit 1
fi

# Create backup directory if it doesn't exist
mkdir -p "$BACKUP_DIR"

# Generate timestamp for backup filename
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="$BACKUP_DIR/maemo_db_backup_${TIMESTAMP}.sql"

# Set PGPASSWORD for pg_dump (non-interactive password)
export PGPASSWORD="$DB_PASSWORD"

echo "Starting database backup..."
echo "Host: $DB_HOST:$DB_PORT"
echo "Database: $DB_NAME"
echo "User: $DB_USER"
echo "Backup file: $BACKUP_FILE"

# Perform backup using pg_dump
# Format: plain SQL (can be restored with psql)
# Options:
#   -Fp: plain format (SQL text)
#   -v: verbose
#   -h: host
#   -p: port
#   -U: user
#   -d: database
#   -f: output file
if pg_dump -Fp -v -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "$BACKUP_FILE"; then
    # Compress the backup file
    echo "Compressing backup file..."
    gzip "$BACKUP_FILE"
    BACKUP_FILE="${BACKUP_FILE}.gz"
    
    # Get file size
    FILE_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
    
    echo "Backup completed successfully!"
    echo "Backup file: $BACKUP_FILE"
    echo "File size: $FILE_SIZE"
    
    # Clean up old backups if retention is set
    if [ "$BACKUP_RETENTION_DAYS" -gt 0 ]; then
        echo "Cleaning up backups older than $BACKUP_RETENTION_DAYS days..."
        find "$BACKUP_DIR" -name "maemo_db_backup_*.sql.gz" -type f -mtime +$BACKUP_RETENTION_DAYS -delete
        echo "Cleanup completed."
    fi
    
    exit 0
else
    echo "ERROR: Backup failed!" >&2
    exit 1
fi

