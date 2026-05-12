import { Injectable, signal, inject } from '@angular/core';
import { ConsultantService } from './consultant.service';
import { ConsultantBranding } from '../models/consultant.model';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class BrandingService {
  private consultantService = inject(ConsultantService);
  private authService = inject(AuthService);

  branding = signal<ConsultantBranding | null>(null);
  isConsultant = signal<boolean>(false);

  constructor() {
    // Try to load branding from localStorage on initialization
    const storedBranding = localStorage.getItem('consultant_branding');
    if (storedBranding) {
      try {
        const branding = JSON.parse(storedBranding);
        this.branding.set(branding);
        this.isConsultant.set(true);
        this.applyBranding(branding);
      } catch (e) {
        // Invalid stored branding, ignore
      }
    }
  }

  async loadBranding(): Promise<void> {
    try {
      // Check if user is consultant (this would come from auth service)
      // For now, try to load branding and catch if not consultant
      const branding = await this.consultantService.getBranding().toPromise();
      if (branding) {
        this.branding.set(branding);
        this.isConsultant.set(true);
        this.applyBranding(branding);
      }
    } catch (error) {
      // Not a consultant or branding not found
      this.isConsultant.set(false);
      this.branding.set(null);
    }
  }

  applyBranding(branding: ConsultantBranding): void {
    const root = document.documentElement;
    
    if (branding.primaryColor) {
      root.style.setProperty('--primary-color', branding.primaryColor);
    }
    
    if (branding.secondaryColor) {
      root.style.setProperty('--secondary-color', branding.secondaryColor);
    }

    // Store branding in localStorage for persistence
    localStorage.setItem('consultant_branding', JSON.stringify(branding));
  }

  getBranding(): ConsultantBranding | null {
    return this.branding();
  }

  clearBranding(): void {
    this.branding.set(null);
    this.isConsultant.set(false);
    localStorage.removeItem('consultant_branding');
    
    // Reset CSS variables
    const root = document.documentElement;
    root.style.removeProperty('--primary-color');
    root.style.removeProperty('--secondary-color');
  }
}

