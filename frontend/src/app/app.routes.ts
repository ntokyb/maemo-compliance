import { Routes } from '@angular/router';
import { tenantGuard } from './guards/tenant.guard';
import { tenantModuleGuard } from './guards/tenant-module.guard';
import { userOnboardingGuard } from './guards/user-onboarding.guard';
import { onboardingPageGuard } from './guards/onboarding-page.guard';
import { platformAdminGuard } from './guards/platform-admin.guard';
import { portalAuthGuard } from './guards/portal-auth.guard';
import { companySetupGuard } from './guards/company-setup.guard';

export const routes: Routes = [
  {
    path: 'accept-invite',
    loadComponent: () =>
      import('./features/public/accept-invite/accept-invite.component').then(m => m.AcceptInviteComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./components/login/login').then(m => m.Login)
  },
  {
    path: 'tenant-selector',
    loadComponent: () => import('./components/tenant-selector/tenant-selector').then(m => m.TenantSelectorComponent)
  },
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/public/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'about',
    loadComponent: () => import('./features/public/about/about.component').then(m => m.AboutComponent)
  },
  {
    path: 'contact',
    loadComponent: () => import('./features/public/contact/contact.component').then(m => m.ContactComponent)
  },
  {
    path: 'signup',
    loadComponent: () => import('./features/public/signup/signup.component').then((m) => m.SignupComponent),
  },
  {
    path: 'verify',
    loadComponent: () =>
      import('./features/public/verify-email/verify-email.component').then((m) => m.VerifyEmailComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/public/forgot-password/forgot-password.component').then((m) => m.ForgotPasswordComponent),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/public/reset-password/reset-password.component').then((m) => m.ResetPasswordComponent),
  },
  {
    path: 'setup',
    loadComponent: () =>
      import('./features/setup/company-setup.component').then((m) => m.CompanySetupComponent),
    canActivate: [portalAuthGuard, tenantGuard],
  },
  {
    path: 'account-pending',
    loadComponent: () =>
      import('./features/public/account-pending/account-pending.component').then((m) => m.AccountPendingComponent),
    canActivate: [portalAuthGuard],
  },
  {
    path: 'onboarding',
    loadComponent: () =>
      import('./features/onboarding/user-onboarding-page/user-onboarding-page.component').then(
        m => m.UserOnboardingPageComponent
      ),
    canActivate: [portalAuthGuard, tenantGuard, onboardingPageGuard]
  },
  {
    path: '',
    loadComponent: () => import('./components/layout/layout').then(m => m.Layout),
    canActivate: [portalAuthGuard, tenantGuard, companySetupGuard, userOnboardingGuard],
    children: [
      {
        path: '',
        redirectTo: '/dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./components/dashboard/dashboard').then(m => m.Dashboard)
      },
      {
        path: 'documents',
        loadComponent: () =>
          import('./features/documents/documents-list/documents-list').then(m => m.DocumentsListComponent),
        canActivate: [tenantModuleGuard('Documents')]
      },
      {
        path: 'documents/new',
        loadComponent: () =>
          import('./features/documents/document-form/document-form').then(m => m.DocumentFormComponent),
        canActivate: [tenantModuleGuard('Documents')]
      },
      {
        path: 'documents/:id',
        loadComponent: () =>
          import('./features/documents/document-form/document-form').then(m => m.DocumentFormComponent),
        canActivate: [tenantModuleGuard('Documents')]
      },
      {
        path: 'documents/approvals',
        loadComponent: () =>
          import('./features/documents/approval-dashboard/approval-dashboard').then(
            m => m.ApprovalDashboardComponent
          ),
        canActivate: [tenantModuleGuard('Documents')]
      },
      {
        path: 'ncrs',
        loadComponent: () => import('./features/ncrs/ncr-list/ncr-list').then(m => m.NcrListComponent),
        canActivate: [tenantModuleGuard('NCR')]
      },
      {
        path: 'ncrs/new',
        loadComponent: () =>
          import('./features/ncrs/ncr-form-offline/ncr-form-offline').then(m => m.NcrFormOfflineComponent),
        canActivate: [tenantModuleGuard('NCR')]
      },
      {
        path: 'ncrs/:id/status',
        loadComponent: () =>
          import('./features/ncrs/ncr-status-update/ncr-status-update').then(m => m.NcrStatusUpdateComponent),
        canActivate: [tenantModuleGuard('NCR')]
      },
      {
        path: 'risks',
        loadComponent: () => import('./features/risks/risk-list/risk-list').then(m => m.RiskListComponent),
        canActivate: [tenantModuleGuard('Risks')]
      },
      {
        path: 'risks/new',
        loadComponent: () => import('./features/risks/risk-form/risk-form').then(m => m.RiskFormComponent),
        canActivate: [tenantModuleGuard('Risks')]
      },
      {
        path: 'risks/:id',
        loadComponent: () => import('./features/risks/risk-form/risk-form').then(m => m.RiskFormComponent),
        canActivate: [tenantModuleGuard('Risks')]
      },
      {
        path: 'audits/templates/new',
        redirectTo: '/consultant/audit-templates',
        pathMatch: 'full'
      },
      {
        path: 'admin',
        children: [
          {
            path: 'access-requests',
            loadComponent: () =>
              import('./admin/access-requests/access-requests.component').then(m => m.AccessRequestsComponent),
            canActivate: [platformAdminGuard]
          },
          {
            path: 'tenant-settings',
            loadComponent: () =>
              import('./features/tenant-admin/tenant-settings/tenant-settings').then(m => m.TenantSettingsComponent)
          },
          {
            path: 'm365-connection',
            loadComponent: () =>
              import('./features/tenant-admin/m365-connection/m365-connection').then(m => m.M365ConnectionComponent)
          },
          {
            path: 'billing-info',
            loadComponent: () =>
              import('./features/tenant-admin/billing-info/billing-info').then(m => m.BillingInfoComponent)
          },
          {
            path: 'popia-report',
            loadComponent: () => import('./features/admin/popia-report/popia-report').then(m => m.PopiaReportComponent)
          },
          {
            path: 'records-retention',
            loadComponent: () =>
              import('./features/admin/records-retention/records-retention').then(m => m.RecordsRetentionComponent)
          }
        ]
      },
      {
        path: 'consultant',
        children: [
          {
            path: '',
            redirectTo: '/consultant/dashboard',
            pathMatch: 'full'
          },
          {
            path: 'dashboard',
            loadComponent: () =>
              import('./features/consultant/consultant-dashboard/consultant-dashboard').then(
                m => m.ConsultantDashboardComponent
              )
          },
          {
            path: 'clients',
            loadComponent: () =>
              import('./features/consultant/consultant-clients/consultant-clients').then(
                m => m.ConsultantClientsComponent
              )
          },
          {
            path: 'branding',
            loadComponent: () =>
              import('./features/consultant/consultant-branding/consultant-branding').then(
                m => m.ConsultantBrandingComponent
              )
          },
          {
            path: 'audit-templates',
            loadComponent: () =>
              import('./features/consultant/audit-templates/audit-templates').then(m => m.AuditTemplatesComponent),
            canActivate: [tenantModuleGuard('Audits')]
          },
          {
            path: 'audit-run',
            loadComponent: () =>
              import('./features/consultant/audit-run/audit-run').then(m => m.AuditRunComponent),
            canActivate: [tenantModuleGuard('Audits')]
          },
          {
            path: 'audit-run/:runId',
            loadComponent: () =>
              import('./features/consultant/audit-run/audit-run').then(m => m.AuditRunComponent),
            canActivate: [tenantModuleGuard('Audits')]
          }
        ]
      }
    ]
  },
  {
    path: '**',
    redirectTo: '/login'
  }
];
