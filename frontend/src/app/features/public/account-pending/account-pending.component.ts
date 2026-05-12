import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { PublicHeaderComponent } from '../public-header/public-header.component';
import { PublicFooterComponent } from '../public-footer/public-footer.component';

@Component({
  selector: 'app-account-pending',
  standalone: true,
  imports: [CommonModule, RouterLink, PublicHeaderComponent, PublicFooterComponent],
  template: `
    <div class="public-page">
      <app-public-header />
      <main class="contain">
        <h1>Workspace under review</h1>
        <p class="lead">
          Thank you for choosing Maemo Compliance. Your Growth workspace is waiting for approval from our team.
          We typically respond within one business day.
        </p>
        <p>
          Signed in as <strong>{{ email }}</strong
          >.
        </p>
        <p>
          <a routerLink="/login" (click)="signOut()">Use a different account</a>
        </p>
      </main>
      <app-public-footer />
    </div>
  `,
  styles: [
    `
      .public-page {
        min-height: 100vh;
        display: flex;
        flex-direction: column;
        font-family: system-ui, -apple-system, sans-serif;
        background: #fff;
      }
      .contain {
        flex: 1;
        max-width: 640px;
        margin: 0 auto;
        padding: 2rem 1.25rem;
      }
      .lead {
        color: #374151;
        line-height: 1.6;
      }
    `
  ]
})
export class AccountPendingComponent {
  private auth = inject(AuthService);

  get email(): string {
    return this.auth.getUser()?.email ?? this.auth.getCurrentUser()?.username ?? '';
  }

  signOut(): void {
    this.auth.logoutLocal();
  }
}
