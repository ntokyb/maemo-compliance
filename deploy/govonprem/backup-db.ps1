# PostgreSQL Database Backup Script for Maemo GovOnPrem (Windows)
# 
# This script creates a timestamped backup of the Maemo database.
# 
# Environment Variables Required:
#   $env:DB_HOST - PostgreSQL host (default: localhost)
#   $env:DB_NAME - Database name (default: maemo_gov)
#   $env:DB_USER - Database user (default: maemo_user)
#   $env:DB_PASSWORD - Database password (required)
#
# Optional Environment Variables:
#   $env:DB_PORT - PostgreSQL port (default: 5432)
#   $env:BACKUP_DIR - Directory to store backups (default: .\backups)
#   $env:BACKUP_RETENTION_DAYS - Number of days to keep backups (default: 30)
#

param(
    [string]$DB_HOST = $env:DB_HOST,
    [string]$DB_PORT = $env:DB_PORT,
    [string]$DB_NAME = $env:DB_NAME,
    [string]$DB_USER = $env:DB_USER,
    [string]$DB_PASSWORD = $env:DB_PASSWORD,
    [string]$BACKUP_DIR = $env:BACKUP_DIR,
    [int]$BACKUP_RETENTION_DAYS = $env:BACKUP_RETENTION_DAYS
)

# Set defaults
if ([string]::IsNullOrEmpty($DB_HOST)) { $DB_HOST = "localhost" }
if ([string]::IsNullOrEmpty($DB_PORT)) { $DB_PORT = "5432" }
if ([string]::IsNullOrEmpty($DB_NAME)) { $DB_NAME = "maemo_gov" }
if ([string]::IsNullOrEmpty($DB_USER)) { $DB_USER = "maemo_user" }
if ([string]::IsNullOrEmpty($BACKUP_DIR)) { $BACKUP_DIR = ".\backups" }
if ($BACKUP_RETENTION_DAYS -eq 0) { $BACKUP_RETENTION_DAYS = 30 }

# Validate required environment variables
if ([string]::IsNullOrEmpty($DB_PASSWORD)) {
    Write-Error "ERROR: DB_PASSWORD environment variable is required"
    exit 1
}

# Check if pg_dump is available
$pgDumpPath = Get-Command pg_dump -ErrorAction SilentlyContinue
if (-not $pgDumpPath) {
    Write-Error "ERROR: pg_dump not found. Please ensure PostgreSQL client tools are installed and in PATH."
    exit 1
}

# Create backup directory if it doesn't exist
if (-not (Test-Path $BACKUP_DIR)) {
    New-Item -ItemType Directory -Path $BACKUP_DIR -Force | Out-Null
}

# Generate timestamp for backup filename
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BACKUP_DIR "maemo_db_backup_${timestamp}.sql"

Write-Host "Starting database backup..."
Write-Host "Host: ${DB_HOST}:${DB_PORT}"
Write-Host "Database: $DB_NAME"
Write-Host "User: $DB_USER"
Write-Host "Backup file: $backupFile"

# Set PGPASSWORD environment variable for pg_dump
$env:PGPASSWORD = $DB_PASSWORD

try {
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
    & pg_dump -Fp -v -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f $backupFile
    
    if ($LASTEXITCODE -eq 0) {
        # Compress the backup file using .NET compression
        Write-Host "Compressing backup file..."
        $compressedFile = "${backupFile}.gz"
        
        $inputFile = [System.IO.File]::OpenRead($backupFile)
        $outputFile = [System.IO.File]::Create($compressedFile)
        $gzipStream = New-Object System.IO.Compression.GzipStream($outputFile, [System.IO.Compression.CompressionMode]::Compress)
        
        $inputFile.CopyTo($gzipStream)
        
        $gzipStream.Close()
        $outputFile.Close()
        $inputFile.Close()
        
        # Remove uncompressed file
        Remove-Item $backupFile
        
        # Get file size
        $fileSize = (Get-Item $compressedFile).Length / 1MB
        $fileSizeFormatted = "{0:N2} MB" -f $fileSize
        
        Write-Host "Backup completed successfully!"
        Write-Host "Backup file: $compressedFile"
        Write-Host "File size: $fileSizeFormatted"
        
        # Clean up old backups if retention is set
        if ($BACKUP_RETENTION_DAYS -gt 0) {
            Write-Host "Cleaning up backups older than $BACKUP_RETENTION_DAYS days..."
            $cutoffDate = (Get-Date).AddDays(-$BACKUP_RETENTION_DAYS)
            Get-ChildItem -Path $BACKUP_DIR -Filter "maemo_db_backup_*.sql.gz" | 
                Where-Object { $_.LastWriteTime -lt $cutoffDate } | 
                Remove-Item -Force
            Write-Host "Cleanup completed."
        }
        
        exit 0
    } else {
        Write-Error "ERROR: Backup failed!"
        exit 1
    }
} catch {
    Write-Error "ERROR: Backup failed with exception: $_"
    exit 1
} finally {
    # Clear PGPASSWORD from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

