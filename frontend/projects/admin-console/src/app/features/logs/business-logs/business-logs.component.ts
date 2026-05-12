import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, JsonPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BusinessLogsAdminService, BusinessLogsFilters } from '../../../core/services/business-logs-admin.service';
import { TenantsAdminService } from '../../../core/services/tenants-admin.service';
import { BusinessAuditLogDto } from '../../../core/models/admin-business-log.dto';
import { AdminTenantListItemDto } from '../../../core/models/admin-tenant.dto';

@Component({
  selector: 'app-business-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, JsonPipe],
  templateUrl: './business-logs.component.html',
  styleUrl: './business-logs.component.scss'
})
export class BusinessLogsComponent implements OnInit {
  private businessLogsService = inject(BusinessLogsAdminService);
  private tenantsService = inject(TenantsAdminService);

  logs: BusinessAuditLogDto[] = [];
  tenants: AdminTenantListItemDto[] = [];
  loading = false;
  error: string | null = null;

  // Filters
  selectedTenantId: string = '';
  selectedAction: string = '';
  selectedEntityType: string = '';
  limit: number = 100;

  // Available options for filters
  availableActions: string[] = [];
  availableEntityTypes: string[] = [
    'Document',
    'NCR',
    'Risk',
    'AuditTemplate',
    'AuditRun',
    'Tenant'
  ];

  // Expanded metadata rows
  expandedMetadata: Set<string> = new Set();

  ngOnInit(): void {
    this.loadTenants();
    this.loadLogs();
  }

  loadTenants(): void {
    this.tenantsService.getTenants().subscribe({
      next: (tenants) => {
        this.tenants = tenants;
      },
      error: (err) => {
        console.error('Failed to load tenants:', err);
      }
    });
  }

  loadLogs(): void {
    this.loading = true;
    this.error = null;

    const filters: BusinessLogsFilters = {
      limit: this.limit
    };

    if (this.selectedTenantId) {
      filters.tenantId = this.selectedTenantId;
    }
    if (this.selectedAction) {
      filters.action = this.selectedAction;
    }
    if (this.selectedEntityType) {
      filters.entityType = this.selectedEntityType;
    }

    this.businessLogsService.getBusinessLogs(filters).subscribe({
      next: (logs) => {
        this.logs = logs;
        
        // Extract unique actions from logs for filter dropdown
        const actions = new Set<string>();
        logs.forEach(log => actions.add(log.action));
        this.availableActions = Array.from(actions).sort();
        
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load business logs';
        this.loading = false;
        console.error('Error loading business logs:', err);
      }
    });
  }

  applyFilters(): void {
    this.loadLogs();
  }

  clearFilters(): void {
    this.selectedTenantId = '';
    this.selectedAction = '';
    this.selectedEntityType = '';
    this.limit = 100;
    this.loadLogs();
  }

  toggleMetadata(logId: string): void {
    if (this.expandedMetadata.has(logId)) {
      this.expandedMetadata.delete(logId);
    } else {
      this.expandedMetadata.add(logId);
    }
  }

  isMetadataExpanded(logId: string): boolean {
    return this.expandedMetadata.has(logId);
  }

  getActionBadgeClass(action: string): string {
    if (action.includes('Created') || action.includes('Started')) {
      return 'badge-success';
    }
    if (action.includes('Updated') || action.includes('Changed')) {
      return 'badge-warning';
    }
    if (action.includes('Deleted') || action.includes('Closed')) {
      return 'badge-danger';
    }
    if (action.includes('Completed')) {
      return 'badge-success';
    }
    if (action.includes('VersionCreated')) {
      return 'badge-info';
    }
    return 'badge-secondary';
  }

  formatMetadata(metadataJson: string | null | undefined): any {
    if (!metadataJson) {
      return null;
    }
    try {
      return JSON.parse(metadataJson);
    } catch {
      return null;
    }
  }

  getTenantName(tenantId: string | null | undefined): string {
    if (!tenantId) {
      return '-';
    }
    const tenant = this.tenants.find(t => t.id === tenantId);
    return tenant?.name || tenantId;
  }
}

