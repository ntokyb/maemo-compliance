# File Storage Restore Script for Maemo GovOnPrem (Windows)
# 
# This script restores the local file storage directory from a backup archive.
# 
# WARNING: This will overwrite existing files in the storage directory!
# 
# Usage:
#   .\restore-files.ps1 <backup_file>
# 
# Environment Variables Required:
#   $env:STORAGE_PATH - Path to local storage directory (default: C:\MaemoFiles)
#
# Optional Environment Variables:
#   $env:BACKUP_DIR - Directory containing backups (default: .\backups)
#

param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    [string]$STORAGE_PATH = $env:STORAGE_PATH,
    [string]$BACKUP_DIR = $env:BACKUP_DIR
)

# Set defaults
if ([string]::IsNullOrEmpty($STORAGE_PATH)) { $STORAGE_PATH = "C:\MaemoFiles" }
if ([string]::IsNullOrEmpty($BACKUP_DIR)) { $BACKUP_DIR = ".\backups" }

# Check if backup file exists
if (-not (Test-Path $BackupFile)) {
    Write-Error "ERROR: Backup file not found: $BackupFile"
    exit 1
}

# Validate backup file is a zip archive
$fileExtension = [System.IO.Path]::GetExtension($BackupFile)
if ($fileExtension -ne ".zip") {
    Write-Error "ERROR: Backup file must be a .zip archive: $BackupFile"
    exit 1
}

Write-Host "Starting file storage restore..."
Write-Host "Storage path: $STORAGE_PATH"
Write-Host "Backup file: $BackupFile"

# Confirm restore operation
Write-Host ""
Write-Host "WARNING: This operation will overwrite existing files in the storage directory!"
$confirm = Read-Host "Are you sure you want to continue? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Restore cancelled."
    exit 0
}

try {
    # Create storage directory if it doesn't exist
    if (-not (Test-Path $STORAGE_PATH)) {
        New-Item -ItemType Directory -Path $STORAGE_PATH -Force | Out-Null
    }
    
    # Backup existing directory if it exists and has content
    if ((Test-Path $STORAGE_PATH) -and ((Get-ChildItem $STORAGE_PATH -Force).Count -gt 0)) {
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $existingBackup = Join-Path $BACKUP_DIR "existing_files_before_restore_${timestamp}.zip"
        Write-Host "Backing up existing files to: $existingBackup"
        
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory($STORAGE_PATH, $existingBackup, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    }
    
    # Extract archive
    Write-Host "Extracting archive..."
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    
    # Remove existing files if directory exists
    if (Test-Path $STORAGE_PATH) {
        Remove-Item "$STORAGE_PATH\*" -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    # Extract zip file
    [System.IO.Compression.ZipFile]::ExtractToDirectory($BackupFile, $STORAGE_PATH)
    
    # Get number of files restored
    $zipArchive = [System.IO.Compression.ZipFile]::OpenRead($BackupFile)
    $fileCount = $zipArchive.Entries.Count
    $zipArchive.Dispose()
    
    Write-Host "Restore completed successfully!"
    Write-Host "Files restored: $fileCount"
    Write-Host "Storage path: $STORAGE_PATH"
    
    exit 0
} catch {
    Write-Error "ERROR: Restore failed with exception: $_"
    exit 1
}

