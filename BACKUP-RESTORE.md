# Backup and Restore Guide for Maemo GovOnPrem

This guide explains how to perform backups and restores for Maemo government on-premises deployments.

## Overview

Maemo GovOnPrem deployments require regular backups of:
1. **Database** - PostgreSQL database containing all application data
2. **File Storage** - Local file storage directory containing uploaded documents and audit evidence

## Prerequisites

### Linux/Unix Systems

- PostgreSQL client tools installed (`pg_dump`, `psql`)
- `tar` and `gzip` utilities
- Bash shell
- Appropriate file system permissions

### Windows Systems

- PostgreSQL client tools installed (`pg_dump.exe`, `psql.exe`)
- PowerShell 5.1 or later
- Appropriate file system permissions

## Backup Scripts

All backup scripts are located in the `deploy/govonprem/` directory.

### Database Backup

#### Linux/Unix (`backup-db.sh`)

Creates a compressed SQL dump of the PostgreSQL database.

**Required Environment Variables:**
- `DB_PASSWORD` - PostgreSQL database password

**Optional Environment Variables:**
- `DB_HOST` - Database host (default: `localhost`)
- `DB_PORT` - Database port (default: `5432`)
- `DB_NAME` - Database name (default: `maemo_gov`)
- `DB_USER` - Database user (default: `maemo_user`)
- `BACKUP_DIR` - Backup directory (default: `./backups`)
- `BACKUP_RETENTION_DAYS` - Days to keep backups (default: `30`)

**Usage:**
```bash
cd deploy/govonprem
chmod +x backup-db.sh
export DB_PASSWORD="your-database-password"
./backup-db.sh
```

**Output:**
- Creates `maemo_db_backup_YYYYMMDD_HHMMSS.sql.gz` in the backup directory

#### Windows (`backup-db.ps1`)

**Usage:**
```powershell
cd deploy\govonprem
$env:DB_PASSWORD = "your-database-password"
.\backup-db.ps1
```

### File Storage Backup

#### Linux/Unix (`backup-files.sh`)

Creates a compressed tar.gz archive of the local file storage directory.

**Required Environment Variables:**
- `STORAGE_PATH` - Path to storage directory (default: `/var/maemo/files`)

**Optional Environment Variables:**
- `BACKUP_DIR` - Backup directory (default: `./backups`)
- `BACKUP_RETENTION_DAYS` - Days to keep backups (default: `30`)

**Usage:**
```bash
cd deploy/govonprem
chmod +x backup-files.sh
export STORAGE_PATH="/var/maemo/files"
./backup-files.sh
```

**Output:**
- Creates `maemo_files_backup_YYYYMMDD_HHMMSS.tar.gz` in the backup directory

#### Windows (`backup-files.ps1`)

**Usage:**
```powershell
cd deploy\govonprem
$env:STORAGE_PATH = "C:\MaemoFiles"
.\backup-files.ps1
```

**Output:**
- Creates `maemo_files_backup_YYYYMMDD_HHMMSS.zip` in the backup directory

## Restore Scripts

### Database Restore

#### Linux/Unix (`restore-db.sh`)

**WARNING:** This will overwrite the existing database!

**Usage:**
```bash
cd deploy/govonprem
chmod +x restore-db.sh
export DB_PASSWORD="your-database-password"
./restore-db.sh backups/maemo_db_backup_20240101_120000.sql.gz
```

**Optional Environment Variables:**
- `DROP_EXISTING` - Set to `"true"` to drop existing database before restore (default: `false`)

#### Windows (`restore-db.ps1`)

**Usage:**
```powershell
cd deploy\govonprem
$env:DB_PASSWORD = "your-database-password"
.\restore-db.ps1 backups\maemo_db_backup_20240101_120000.sql.gz
```

### File Storage Restore

#### Linux/Unix (`restore-files.sh`)

**WARNING:** This will overwrite existing files in the storage directory!

**Usage:**
```bash
cd deploy/govonprem
chmod +x restore-files.sh
export STORAGE_PATH="/var/maemo/files"
./restore-files.sh backups/maemo_files_backup_20240101_120000.tar.gz
```

**Note:** The script automatically backs up existing files before restore.

#### Windows (`restore-files.ps1`)

**Usage:**
```powershell
cd deploy\govonprem
$env:STORAGE_PATH = "C:\MaemoFiles"
.\restore-files.ps1 backups\maemo_files_backup_20240101_120000.zip
```

## Backup Storage

### Recommended Backup Location

Store backups in a secure, off-site location separate from the application server:

- **On-premises:** Network-attached storage (NAS) or dedicated backup server
- **Cloud:** Secure cloud storage with encryption (if permitted by security policy)
- **Removable Media:** Encrypted external drives or tapes (for air-gapped environments)

### Backup Directory Structure

```
/backups/maemo/
├── database/
│   ├── maemo_db_backup_20240101_120000.sql.gz
│   ├── maemo_db_backup_20240102_120000.sql.gz
│   └── ...
└── files/
    ├── maemo_files_backup_20240101_120000.tar.gz
    ├── maemo_files_backup_20240108_120000.tar.gz
    └── ...
```

## Recommended Backup Schedule

### Database Backups

- **Frequency:** Daily (or more frequently for high-transaction environments)
- **Retention:** 30 days minimum (adjust based on compliance requirements)
- **Best Time:** During low-usage periods (e.g., 2:00 AM)

**Example Cron Schedule (Linux):**
```cron
# Daily database backup at 2:00 AM
0 2 * * * cd /opt/maemo/deploy/govonprem && DB_PASSWORD="your-password" ./backup-db.sh
```

**Example Task Scheduler (Windows):**
- Create a scheduled task to run `backup-db.ps1` daily at 2:00 AM
- Set environment variables in the task's action settings

### File Storage Backups

- **Frequency:** Weekly (or daily if high file upload volume)
- **Retention:** 90 days minimum (files change less frequently than database)
- **Best Time:** During low-usage periods (e.g., Sunday 3:00 AM)

**Example Cron Schedule (Linux):**
```cron
# Weekly file storage backup on Sunday at 3:00 AM
0 3 * * 0 cd /opt/maemo/deploy/govonprem && STORAGE_PATH="/var/maemo/files" ./backup-files.sh
```

**Example Task Scheduler (Windows):**
- Create a scheduled task to run `backup-files.ps1` weekly on Sunday at 3:00 AM

## Backup Verification

### Verify Database Backup

```bash
# Check backup file exists and is not empty
ls -lh backups/maemo_db_backup_*.sql.gz

# Verify backup file integrity (for compressed files)
gunzip -t backups/maemo_db_backup_YYYYMMDD_HHMMSS.sql.gz

# Test restore to a test database (optional)
export DB_NAME="maemo_test"
./restore-db.sh backups/maemo_db_backup_YYYYMMDD_HHMMSS.sql.gz
```

### Verify File Storage Backup

```bash
# List contents of backup archive
tar -tzf backups/maemo_files_backup_YYYYMMDD_HHMMSS.tar.gz | head -20

# Verify archive integrity
tar -tzf backups/maemo_files_backup_YYYYMMDD_HHMMSS.tar.gz > /dev/null && echo "Archive is valid"
```

## Disaster Recovery Procedure

### Full System Restore

1. **Stop the application:**
   ```bash
   systemctl stop maemo-api
   systemctl stop maemo-workers
   ```

2. **Restore database:**
   ```bash
   cd deploy/govonprem
   export DB_PASSWORD="your-password"
   export DROP_EXISTING="true"
   ./restore-db.sh backups/maemo_db_backup_YYYYMMDD_HHMMSS.sql.gz
   ```

3. **Restore file storage:**
   ```bash
   export STORAGE_PATH="/var/maemo/files"
   ./restore-files.sh backups/maemo_files_backup_YYYYMMDD_HHMMSS.tar.gz
   ```

4. **Verify permissions:**
   ```bash
   chown -R maemo:maemo /var/maemo/files
   chmod -R 755 /var/maemo/files
   ```

5. **Start the application:**
   ```bash
   systemctl start maemo-api
   systemctl start maemo-workers
   ```

6. **Verify application health:**
   ```bash
   curl http://localhost:5000/health/ready
   ```

## Security Considerations

1. **Encryption:**
   - Store backups in encrypted format
   - Use encrypted transport for off-site backups
   - Encrypt removable media

2. **Access Control:**
   - Restrict backup directory permissions
   - Use secure credentials management for database passwords
   - Limit access to backup scripts

3. **Audit Trail:**
   - Log all backup and restore operations
   - Maintain backup logs with timestamps and file sizes
   - Document restore procedures and test them regularly

4. **Compliance:**
   - Ensure backups meet retention requirements
   - Verify backups are stored according to data classification policies
   - Document backup procedures for audit purposes

## Troubleshooting

### Database Backup Fails

**Error: "pg_dump: connection to database failed"**
- Verify database is running: `systemctl status postgresql`
- Check connection parameters (host, port, user, password)
- Verify network connectivity to database server
- Check PostgreSQL logs: `/var/log/postgresql/postgresql-*.log`

**Error: "Permission denied"**
- Ensure database user has necessary permissions
- Check file system permissions for backup directory

### File Storage Backup Fails

**Error: "Storage directory not found"**
- Verify `STORAGE_PATH` environment variable is set correctly
- Check that the directory exists and is accessible
- Verify file system permissions

**Error: "Permission denied"**
- Ensure script has read permissions for storage directory
- Check write permissions for backup directory

### Restore Fails

**Error: "Database already exists"**
- Set `DROP_EXISTING="true"` environment variable
- Or manually drop the database before restore

**Error: "Out of disk space"**
- Check available disk space: `df -h`
- Clean up old backups if needed
- Ensure sufficient space for both backup and restore operations

## Additional Resources

- PostgreSQL Backup Documentation: https://www.postgresql.org/docs/current/backup.html
- Linux tar/gzip Documentation: `man tar`, `man gzip`
- PowerShell Documentation: https://docs.microsoft.com/powershell/

## Support

For issues or questions regarding backup and restore procedures, contact your system administrator or Maemo support team.

