# PostgreSQL Database Restore Script for Maemo GovOnPrem (Windows)
# 
# This script restores a Maemo database from a backup file.
# 
# WARNING: This will overwrite the existing database!
# 
# Usage:
#   .\restore-db.ps1 <backup_file>
# 
# Environment Variables Required:
#   $env:DB_HOST - PostgreSQL host (default: localhost)
#   $env:DB_NAME - Database name (default: maemo_gov)
#   $env:DB_USER - Database user (default: maemo_user)
#   $env:DB_PASSWORD - Database password (required)
#
# Optional Environment Variables:
#   $env:DB_PORT - PostgreSQL port (default: 5432)
#   $env:DROP_EXISTING - Set to "true" to drop existing database before restore (default: false)
#

param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    [string]$DB_HOST = $env:DB_HOST,
    [string]$DB_PORT = $env:DB_PORT,
    [string]$DB_NAME = $env:DB_NAME,
    [string]$DB_USER = $env:DB_USER,
    [string]$DB_PASSWORD = $env:DB_PASSWORD,
    [string]$DROP_EXISTING = $env:DROP_EXISTING
)

# Set defaults
if ([string]::IsNullOrEmpty($DB_HOST)) { $DB_HOST = "localhost" }
if ([string]::IsNullOrEmpty($DB_PORT)) { $DB_PORT = "5432" }
if ([string]::IsNullOrEmpty($DB_NAME)) { $DB_NAME = "maemo_gov" }
if ([string]::IsNullOrEmpty($DB_USER)) { $DB_USER = "maemo_user" }
if ([string]::IsNullOrEmpty($DROP_EXISTING)) { $DROP_EXISTING = "false" }

# Validate required environment variables
if ([string]::IsNullOrEmpty($DB_PASSWORD)) {
    Write-Error "ERROR: DB_PASSWORD environment variable is required"
    exit 1
}

# Check if backup file exists
if (-not (Test-Path $BackupFile)) {
    Write-Error "ERROR: Backup file not found: $BackupFile"
    exit 1
}

# Check if psql is available
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psqlPath) {
    Write-Error "ERROR: psql not found. Please ensure PostgreSQL client tools are installed and in PATH."
    exit 1
}

# Set PGPASSWORD environment variable for psql
$env:PGPASSWORD = $DB_PASSWORD

Write-Host "Starting database restore..."
Write-Host "Host: ${DB_HOST}:${DB_PORT}"
Write-Host "Database: $DB_NAME"
Write-Host "User: $DB_USER"
Write-Host "Backup file: $BackupFile"

# Confirm restore operation
Write-Host ""
Write-Host "WARNING: This operation will overwrite the existing database!"
$confirm = Read-Host "Are you sure you want to continue? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Restore cancelled."
    exit 0
}

# Determine if backup is compressed
$restoreFile = $BackupFile
$tempFile = $false

if ($BackupFile -match '\.gz$') {
    Write-Host "Decompressing backup file..."
    $restoreFile = $BackupFile -replace '\.gz$', ''
    
    $inputFile = [System.IO.File]::OpenRead($BackupFile)
    $outputFile = [System.IO.File]::Create($restoreFile)
    $gzipStream = New-Object System.IO.Compression.GzipStream($inputFile, [System.IO.Compression.CompressionMode]::Decompress)
    
    $gzipStream.CopyTo($outputFile)
    
    $gzipStream.Close()
    $outputFile.Close()
    $inputFile.Close()
    
    $tempFile = $true
}

try {
    # Drop existing database if requested
    if ($DROP_EXISTING -eq "true") {
        Write-Host "Dropping existing database..."
        & psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d postgres -c "DROP DATABASE IF EXISTS $DB_NAME;" 2>&1 | Out-Null
        Write-Host "Creating new database..."
        & psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d postgres -c "CREATE DATABASE $DB_NAME;"
    }
    
    # Restore database
    Write-Host "Restoring database from backup..."
    & psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f $restoreFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database restore completed successfully!"
        
        # Clean up temporary file if we decompressed
        if ($tempFile) {
            Remove-Item $restoreFile -Force
        }
        
        exit 0
    } else {
        Write-Error "ERROR: Restore failed!"
        
        # Clean up temporary file if we decompressed
        if ($tempFile) {
            Remove-Item $restoreFile -Force -ErrorAction SilentlyContinue
        }
        
        exit 1
    }
} catch {
    Write-Error "ERROR: Restore failed with exception: $_"
    
    # Clean up temporary file if we decompressed
    if ($tempFile) {
        Remove-Item $restoreFile -Force -ErrorAction SilentlyContinue
    }
    
    exit 1
} finally {
    # Clear PGPASSWORD from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

