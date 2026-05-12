import { Component, computed, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { EventType, AccountInfo } from '@azure/msal-browser';
import { environment } from '../../../environments/environment';
import { TenantContextService } from '../../services/tenant-context.service';
import { TenantModulesService } from '../../services/tenant-modules.service';
import { BrandingService } from '../../services/branding.service';
import { ConsultantClientService, ConsultantClientDto } from '../../services/consultant-client.service';
import { ToastComponent } from '../../shared/toast/toast.component';
import { OnboardingWizardComponent } from '../onboarding-wizard/onboarding-wizard';
import { OnboardingService } from '../../services/onboarding.service';
import { OfflineBannerComponent } from '../offline-banner/offline-banner';

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule, FormsModule, ToastComponent, OnboardingWizardComponent, OfflineBannerComponent],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class Layout implements OnInit, OnDestroy {
  private msalService = inject(MsalService);
  private msalBroadcastService = inject(MsalBroadcastService);
  private tenantContextService = inject(TenantContextService);
  private tenantModulesService = inject(TenantModulesService);
  private router = inject(Router);
  private brandingService = inject(BrandingService);
  private consultantClientService = inject(ConsultantClientService);
  private onboardingService = inject(OnboardingService);

  activeAccount = signal<AccountInfo | null>(null);
  displayName = computed(() => {
    const a = this.activeAccount();
    return a?.name ?? a?.username ?? 'User';
  });

  private subscriptions = new Subscription();

  showOnboardingWizard = false;

  clients: ConsultantClientDto[] = [];
  selectedClientId: string | null = null;
  loadingClients = false;

  private syncActiveAccount(): void {
    const accounts = this.msalService.instance.getAllAccounts();
    const account = accounts[0] ?? null;
    if (account) {
      this.msalService.instance.setActiveAccount(account);
    }
    this.activeAccount.set(account);
  }

  get currentTenantId() {
    return this.tenantContextService.getTenantId();
  }

  get isConsultant() {
    return this.tenantContextService.isConsultant();
  }

  hasModule(moduleName: string): boolean {
    return this.tenantModulesService.hasModule(moduleName);
  }

  login(): void {
    this.msalService.loginRedirect({
      scopes: [environment.azureAd.apiScope]
    });
  }

  logout(): void {
    this.tenantContextService.clearTenantId();
    this.msalService.logoutRedirect({
      postLogoutRedirectUri: window.location.origin
    });
  }

  changeTenant(): void {
    if (this.isConsultant) {
      // For consultants, client switcher is already visible in dropdown
      // No action needed - dropdown handles client switching
    } else {
      // For regular users, navigate to tenant selector
      this.router.navigate(['/tenant-selector']);
    }
  }

  ngOnInit(): void {
    this.msalService.instance.enableAccountStorageEvents();
    this.subscriptions.add(
      this.msalBroadcastService.msalSubject$
        .pipe(
          filter(
            (msg) =>
              msg.eventType === EventType.LOGIN_SUCCESS ||
              msg.eventType === EventType.ACCOUNT_ADDED ||
              msg.eventType === EventType.ACCOUNT_REMOVED ||
              msg.eventType === EventType.ACTIVE_ACCOUNT_CHANGED
          )
        )
        .subscribe(() => this.syncActiveAccount())
    );
    this.syncActiveAccount();

    // Load tenant modules and branding when tenant is available
    if (this.currentTenantId) {
      this.tenantModulesService.loadModules().subscribe({
        next: () => {
          // Apply branding after modules are loaded
          this.applyBranding();
        }
      });
    } else {
      // Apply default branding if no tenant
      this.applyBranding();
    }

    // Load branding
    this.brandingService.loadBranding().then(() => {
      // Check if user is consultant by trying to initialize client service
      this.checkConsultantStatus();
    });

    // Check onboarding status for non-consultant users
    if (!this.isConsultant && this.currentTenantId) {
      this.checkOnboardingStatus();
    }
  }

  private checkOnboardingStatus(): void {
    this.onboardingService.getStatus().subscribe({
      next: (status) => {
        if (!status.onboardingCompleted) {
          this.showOnboardingWizard = true;
        }
      },
      error: (err) => {
        // If error, assume onboarding not completed (for new tenants)
        console.error('Error checking onboarding status:', err);
        // Don't show wizard on error - let user proceed
      }
    });
  }

  onOnboardingComplete(): void {
    this.showOnboardingWizard = false;
    // Reload page to refresh data
    window.location.reload();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private async checkConsultantStatus(): Promise<void> {
    try {
      // Try to initialize consultant client service - if successful, user is consultant
      this.consultantClientService.initialize();
      
      // Subscribe to clients observable
      this.subscriptions.add(
        this.consultantClientService.clients$.subscribe(clients => {
          if (clients.length >= 0) {
            // User is consultant (even if no clients yet)
            this.tenantContextService.setIsConsultant(true);
            this.clients = clients;
          }
        })
      );

      // Subscribe to active client observable
      this.subscriptions.add(
        this.consultantClientService.activeClient$.subscribe(client => {
          this.selectedClientId = client?.tenantId || null;
        })
      );

      // Subscribe to loading observable
      this.subscriptions.add(
        this.consultantClientService.loading$.subscribe(loading => {
          this.loadingClients = loading;
        })
      );
    } catch (error) {
      // User is not a consultant
      this.tenantContextService.setIsConsultant(false);
    }
  }

  onClientChange(clientId: string): void {
    if (!clientId) {
      return;
    }
    
    const client = this.clients.find(c => c.tenantId === clientId);
    if (client) {
      this.consultantClientService.setActiveClient(client);
      
      // Reload current page to refresh data with new tenant context
      const currentUrl = this.router.url;
      this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
        this.router.navigate([currentUrl]);
      });
    }
  }

  get branding() {
    return this.brandingService.getBranding();
  }

  get isDemoMode() {
    return this.tenantContextService.isDemoMode();
  }

  get demoTenantName() {
    return this.tenantContextService.getDemoTenantName();
  }

  get logoUrl() {
    return this.tenantContextService.getLogoUrl();
  }

  get primaryColor() {
    return this.tenantContextService.getPrimaryColor() || '#1976d2';
  }

  private applyBranding(): void {
    const primaryColor = this.tenantContextService.getPrimaryColor() || '#1976d2';
    document.documentElement.style.setProperty('--maemo-primary-color', primaryColor);
  }
}
