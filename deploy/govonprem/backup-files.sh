#!/bin/bash
#
# File Storage Backup Script for Maemo GovOnPrem
# 
# This script creates a timestamped tar.gz archive of the local file storage directory.
# 
# Environment Variables Required:
#   STORAGE_PATH - Path to local storage directory (default: /var/maemo/files)
#
# Optional Environment Variables:
#   BACKUP_DIR - Directory to store backups (default: ./backups)
#   BACKUP_RETENTION_DAYS - Number of days to keep backups (default: 30)
#

set -euo pipefail

# Default values
STORAGE_PATH="${STORAGE_PATH:-/var/maemo/files}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"

# Validate storage path exists
if [ ! -d "$STORAGE_PATH" ]; then
    echo "ERROR: Storage directory not found: $STORAGE_PATH" >&2
    exit 1
fi

# Create backup directory if it doesn't exist
mkdir -p "$BACKUP_DIR"

# Generate timestamp for backup filename
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="$BACKUP_DIR/maemo_files_backup_${TIMESTAMP}.tar.gz"

echo "Starting file storage backup..."
echo "Source: $STORAGE_PATH"
echo "Backup file: $BACKUP_FILE"

# Perform backup using tar with gzip compression
# Options:
#   -c: create archive
#   -z: compress with gzip
#   -f: output file
#   -C: change to directory before archiving
#   --exclude: exclude patterns (optional)
if tar -czf "$BACKUP_FILE" -C "$(dirname "$STORAGE_PATH")" "$(basename "$STORAGE_PATH")"; then
    # Get file size
    FILE_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
    
    # Get number of files/directories backed up
    FILE_COUNT=$(tar -tzf "$BACKUP_FILE" | wc -l)
    
    echo "Backup completed successfully!"
    echo "Backup file: $BACKUP_FILE"
    echo "File size: $FILE_SIZE"
    echo "Files archived: $FILE_COUNT"
    
    # Clean up old backups if retention is set
    if [ "$BACKUP_RETENTION_DAYS" -gt 0 ]; then
        echo "Cleaning up backups older than $BACKUP_RETENTION_DAYS days..."
        find "$BACKUP_DIR" -name "maemo_files_backup_*.tar.gz" -type f -mtime +$BACKUP_RETENTION_DAYS -delete
        echo "Cleanup completed."
    fi
    
    exit 0
else
    echo "ERROR: Backup failed!" >&2
    exit 1
fi

