import { Routes } from '@angular/router';
import { AdminLayoutComponent } from './layout/admin-layout/admin-layout.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { TenantsListComponent } from './features/tenants/tenants-list.component';
import { TenantDetailComponent } from './features/tenants/tenant-detail.component';
import { WorkersComponent } from './features/workers/workers.component';
import { BusinessLogsComponent } from './features/logs/business-logs/business-logs.component';
import { PopiaComponent } from './features/popia/popia.component';
import { EvidenceRegisterComponent } from './features/evidence/evidence-register.component';
import { DestroyDocumentComponent } from './features/documents/destroy-document.component';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    canActivate: [adminGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'tenants', component: TenantsListComponent },
      { path: 'tenants/:id', component: TenantDetailComponent },
      { path: 'workers', component: WorkersComponent },
      { path: 'logs/business', component: BusinessLogsComponent },
      { path: 'popia', component: PopiaComponent },
      { path: 'evidence', component: EvidenceRegisterComponent },
      { path: 'documents/:id/destroy', component: DestroyDocumentComponent },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];
