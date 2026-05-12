import { Injectable, inject } from '@angular/core';
import { MsalService } from '@azure/msal-angular';

@Injectable({
  providedIn: 'root'
})
export class PlatformAdminService {
  private msal = inject(MsalService);

  isPlatformAdmin(): boolean {
    const account = this.msal.instance.getActiveAccount();
    const raw = account?.idTokenClaims as Record<string, unknown> | undefined;
    const roles = raw?.['roles'];
    const list: string[] = Array.isArray(roles) ? roles.map(String) : [];
    return list.includes('PlatformAdmin') || list.includes('CodistAdmin');
  }
}
