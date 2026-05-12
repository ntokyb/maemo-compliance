import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConsultantService } from '../../../services/consultant.service';
import { ConsultantBranding } from '../../../models/consultant.model';
import { BrandingService } from '../../../services/branding.service';

@Component({
  selector: 'app-consultant-branding',
  imports: [CommonModule, FormsModule],
  templateUrl: './consultant-branding.html',
  styleUrl: './consultant-branding.scss',
})
export class ConsultantBrandingComponent implements OnInit {
  private consultantService = inject(ConsultantService);
  private brandingService = inject(BrandingService);

  branding: ConsultantBranding = {
    logoUrl: '',
    primaryColor: '',
    secondaryColor: '',
    loginBannerUrl: ''
  };

  logoFile: File | null = null;
  bannerFile: File | null = null;
  saving = false;
  error: string | null = null;
  success = false;

  ngOnInit(): void {
    this.loadBranding();
  }

  loadBranding(): void {
    this.consultantService.getBranding().subscribe({
      next: (branding) => {
        this.branding = branding || this.branding;
      },
      error: (err) => {
        console.error('Error loading branding:', err);
        // Branding might not exist yet, that's okay
      }
    });
  }

  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.logoFile = input.files[0];
    }
  }

  onBannerSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.bannerFile = input.files[0];
    }
  }

  async save(): Promise<void> {
    this.saving = true;
    this.error = null;
    this.success = false;

    try {
      // Upload logo if selected
      if (this.logoFile) {
        const logoResult = await this.consultantService.uploadBrandingFile('logo', this.logoFile).toPromise();
        if (logoResult) {
          this.branding.logoUrl = logoResult.url;
        }
      }

      // Upload banner if selected
      if (this.bannerFile) {
        const bannerResult = await this.consultantService.uploadBrandingFile('loginBanner', this.bannerFile).toPromise();
        if (bannerResult) {
          this.branding.loginBannerUrl = bannerResult.url;
        }
      }

      // Update branding
      await this.consultantService.updateBranding(this.branding).toPromise();

      // Apply branding immediately
      this.brandingService.applyBranding(this.branding);

      this.success = true;
      this.logoFile = null;
      this.bannerFile = null;

      // Reset file inputs
      const logoInput = document.getElementById('logo-input') as HTMLInputElement;
      const bannerInput = document.getElementById('banner-input') as HTMLInputElement;
      if (logoInput) logoInput.value = '';
      if (bannerInput) bannerInput.value = '';

      setTimeout(() => {
        this.success = false;
      }, 3000);
    } catch (err: any) {
      this.error = err.message || 'Failed to save branding';
      console.error('Error saving branding:', err);
    } finally {
      this.saving = false;
    }
  }
}

