#!/bin/bash
#
# File Storage Restore Script for Maemo GovOnPrem
# 
# This script restores the local file storage directory from a backup archive.
# 
# WARNING: This will overwrite existing files in the storage directory!
# 
# Usage:
#   restore-files.sh <backup_file>
# 
# Environment Variables Required:
#   STORAGE_PATH - Path to local storage directory (default: /var/maemo/files)
#
# Optional Environment Variables:
#   BACKUP_DIR - Directory containing backups (default: ./backups)
#

set -euo pipefail

# Default values
STORAGE_PATH="${STORAGE_PATH:-/var/maemo/files}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"

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

# Validate backup file is a tar.gz archive
if ! file "$BACKUP_FILE" | grep -q "gzip\|tar"; then
    echo "ERROR: Backup file does not appear to be a tar.gz archive: $BACKUP_FILE" >&2
    exit 1
fi

echo "Starting file storage restore..."
echo "Storage path: $STORAGE_PATH"
echo "Backup file: $BACKUP_FILE"

# Confirm restore operation
echo ""
echo "WARNING: This operation will overwrite existing files in the storage directory!"
read -p "Are you sure you want to continue? (yes/no): " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
    echo "Restore cancelled."
    exit 0
fi

# Create storage directory if it doesn't exist
mkdir -p "$STORAGE_PATH"

# Backup existing directory if it exists and has content
if [ -d "$STORAGE_PATH" ] && [ "$(ls -A "$STORAGE_PATH" 2>/dev/null)" ]; then
    TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
    EXISTING_BACKUP="$BACKUP_DIR/existing_files_before_restore_${TIMESTAMP}.tar.gz"
    echo "Backing up existing files to: $EXISTING_BACKUP"
    tar -czf "$EXISTING_BACKUP" -C "$(dirname "$STORAGE_PATH")" "$(basename "$STORAGE_PATH")" || true
fi

# Extract archive
# Options:
#   -x: extract
#   -z: decompress with gzip
#   -f: input file
#   -C: change to directory before extracting
echo "Extracting archive..."
if tar -xzf "$BACKUP_FILE" -C "$(dirname "$STORAGE_PATH")"; then
    # Get number of files restored
    FILE_COUNT=$(tar -tzf "$BACKUP_FILE" | wc -l)
    
    echo "Restore completed successfully!"
    echo "Files restored: $FILE_COUNT"
    echo "Storage path: $STORAGE_PATH"
    
    # Set appropriate permissions (adjust as needed for your environment)
    echo "Setting permissions..."
    chmod -R 755 "$STORAGE_PATH" || true
    
    exit 0
else
    echo "ERROR: Restore failed!" >&2
    exit 1
fi

