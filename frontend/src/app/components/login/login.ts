import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { TenantContextService } from '../../services/tenant-context.service';
import { BrandingService } from '../../services/branding.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-login',
  imports: [CommonModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private tenantContextService = inject(TenantContextService);
  private brandingService = inject(BrandingService);
  private http = inject(HttpClient);

  demoLoading = false;
  demoError: string | null = null;

  ngOnInit(): void {
    // Load branding if consultant
    this.brandingService.loadBranding();
    
    // If already authenticated, check for tenant selection
    if (this.authService.isAuthenticated()) {
      this.checkTenantAndRedirect();
    }
  }

  get branding() {
    return this.brandingService.getBranding();
  }

  login(): void {
    this.authService.login();
  }

  switchToDemo(): void {
    this.demoLoading = true;
    this.demoError = null;

    this.http.get<{ tenantId: string; tenantName: string; code: string }>(`${environment.apiBaseUrl}/api/demo/tenant`).subscribe({
      next: (response) => {
        this.tenantContextService.setTenantId(response.tenantId);
        this.tenantContextService.setDemoTenantName(response.tenantName);
        this.demoLoading = false;
        // Redirect to dashboard after selecting demo tenant
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.demoLoading = false;
        this.demoError = 'Demo tenant is currently unavailable.';
        console.error('Error loading demo tenant:', err);
      }
    });
  }

  private checkTenantAndRedirect(): void {
    const invite = sessionStorage.getItem('maemo_invite_token');
    if (invite) {
      this.router.navigate(['/accept-invite'], { queryParams: { token: invite } });
      return;
    }

    if (this.tenantContextService.hasTenant()) {
      this.router.navigate(['/dashboard']);
    } else {
      this.router.navigate(['/tenant-selector']);
    }
  }
}
