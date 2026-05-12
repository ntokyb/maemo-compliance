import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../services/auth.service';
import { TenantService } from '../../../services/tenant.service';

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './accept-invite.component.html',
  styleUrl: './accept-invite.component.scss'
})
export class AcceptInviteComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  readonly auth = inject(AuthService);
  private tenantService = inject(TenantService);

  token: string | null = null;
  loading = true;
  acceptLoading = false;
  preview: { valid: boolean; companyName?: string | null; email?: string | null; message?: string | null } | null =
    null;
  acceptError: string | null = null;
  done = false;

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token');
    if (!this.token) {
      this.loading = false;
      this.preview = { valid: false, message: 'Missing invitation token.' };
      return;
    }

    this.http
      .get<{
        valid: boolean;
        companyName?: string | null;
        email?: string | null;
        message?: string | null;
      }>(`${environment.apiBaseUrl}/public/invite/validate`, { params: { token: this.token } })
      .subscribe({
        next: (p) => {
          this.preview = p;
          this.loading = false;
          if (this.auth.isAuthenticated() && p.valid) {
            this.accept();
          }
        },
        error: () => {
          this.loading = false;
          this.preview = { valid: false, message: 'Could not validate invitation.' };
        }
      });
  }

  login(): void {
    if (this.token) {
      sessionStorage.setItem('maemo_invite_token', this.token);
    }
    this.router.navigate(['/login']);
  }

  accept(): void {
    const t = this.token || sessionStorage.getItem('maemo_invite_token');
    if (!t) {
      this.acceptError = 'Missing token.';
      return;
    }
    this.acceptLoading = true;
    this.acceptError = null;
    this.tenantService.acceptInvitation(t).subscribe({
      next: () => {
        sessionStorage.removeItem('maemo_invite_token');
        this.acceptLoading = false;
        this.done = true;
      },
      error: (err) => {
        this.acceptLoading = false;
        this.acceptError = err.error?.message || err.message || 'Could not accept invitation.';
      }
    });
  }

  goDashboard(): void {
    this.router.navigate(['/dashboard']);
  }
}
