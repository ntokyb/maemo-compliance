# File Storage Backup Script for Maemo GovOnPrem (Windows)
# 
# This script creates a timestamped zip archive of the local file storage directory.
# 
# Environment Variables Required:
#   $env:STORAGE_PATH - Path to local storage directory (default: C:\MaemoFiles)
#
# Optional Environment Variables:
#   $env:BACKUP_DIR - Directory to store backups (default: .\backups)
#   $env:BACKUP_RETENTION_DAYS - Number of days to keep backups (default: 30)
#

param(
    [string]$STORAGE_PATH = $env:STORAGE_PATH,
    [string]$BACKUP_DIR = $env:BACKUP_DIR,
    [int]$BACKUP_RETENTION_DAYS = $env:BACKUP_RETENTION_DAYS
)

# Set defaults
if ([string]::IsNullOrEmpty($STORAGE_PATH)) { $STORAGE_PATH = "C:\MaemoFiles" }
if ([string]::IsNullOrEmpty($BACKUP_DIR)) { $BACKUP_DIR = ".\backups" }
if ($BACKUP_RETENTION_DAYS -eq 0) { $BACKUP_RETENTION_DAYS = 30 }

# Validate storage path exists
if (-not (Test-Path $STORAGE_PATH -PathType Container)) {
    Write-Error "ERROR: Storage directory not found: $STORAGE_PATH"
    exit 1
}

# Create backup directory if it doesn't exist
if (-not (Test-Path $BACKUP_DIR)) {
    New-Item -ItemType Directory -Path $BACKUP_DIR -Force | Out-Null
}

# Generate timestamp for backup filename
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BACKUP_DIR "maemo_files_backup_${timestamp}.zip"

Write-Host "Starting file storage backup..."
Write-Host "Source: $STORAGE_PATH"
Write-Host "Backup file: $backupFile"

try {
    # Perform backup using .NET compression (zip format)
    # Remove existing backup file if it exists
    if (Test-Path $backupFile) {
        Remove-Item $backupFile -Force
    }
    
    # Create zip archive
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($STORAGE_PATH, $backupFile, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    
    # Get file size
    $fileSize = (Get-Item $backupFile).Length / 1MB
    $fileSizeFormatted = "{0:N2} MB" -f $fileSize
    
    # Get number of files backed up
    $zipArchive = [System.IO.Compression.ZipFile]::OpenRead($backupFile)
    $fileCount = $zipArchive.Entries.Count
    $zipArchive.Dispose()
    
    Write-Host "Backup completed successfully!"
    Write-Host "Backup file: $backupFile"
    Write-Host "File size: $fileSizeFormatted"
    Write-Host "Files archived: $fileCount"
    
    # Clean up old backups if retention is set
    if ($BACKUP_RETENTION_DAYS -gt 0) {
        Write-Host "Cleaning up backups older than $BACKUP_RETENTION_DAYS days..."
        $cutoffDate = (Get-Date).AddDays(-$BACKUP_RETENTION_DAYS)
        Get-ChildItem -Path $BACKUP_DIR -Filter "maemo_files_backup_*.zip" | 
            Where-Object { $_.LastWriteTime -lt $cutoffDate } | 
            Remove-Item -Force
        Write-Host "Cleanup completed."
    }
    
    exit 0
} catch {
    Write-Error "ERROR: Backup failed with exception: $_"
    exit 1
}

