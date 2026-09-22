import { Component, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ColorPicker } from 'primeng/colorpicker';
import { Toast } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { SharedModule, MessageService } from 'primeng/api';
import { DividerModule } from 'primeng/divider';
import { TenantBrandingService } from '@core/services/tenant-branding.service';
import { UserService } from '@core/services/user.service';

export interface ColorPalette {
  name: string;
  primary: string;
  secondary: string;
}

@Component({
  selector: 'app-employer-branding-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    InputTextModule,
    ColorPicker,
    Toast,
    TooltipModule,
    SharedModule,
    DividerModule,
  ],
  providers: [MessageService],
  templateUrl: './branding.component.html',
})
export class BrandingComponent {
  private brandingService = inject(TenantBrandingService);
  private messageService = inject(MessageService);
  public userService = inject(UserService);

  primaryColor = signal<string>('#6366F1');
  secondaryColor = signal<string>('#10B981');
  previewMode = signal<'light' | 'dark'>('light');

  logoFile = signal<File | null>(null);
  logoFileDark = signal<File | null>(null);
  faviconFile = signal<File | null>(null);

  logoPreviewUrl = signal<string | null>(null);
  logoDarkPreviewUrl = signal<string | null>(null);

  isSaving = signal(false);

  suggestedPalettes: ColorPalette[] = [
    { name: 'Indigo', primary: '#6366F1', secondary: '#8B5CF6' },
    { name: 'Sky', primary: '#0EA5E9', secondary: '#38BDF8' },
    { name: 'Emerald', primary: '#10B981', secondary: '#34D399' },
    { name: 'Amber', primary: '#F59E0B', secondary: '#FBBF24' },
    { name: 'Rose', primary: '#F43F5E', secondary: '#FB7185' },
  ];

  constructor() {
    effect(() => {
      const current = this.brandingService.currentBranding();
      if (current) {
        if (current.primaryColor) this.primaryColor.set(current.primaryColor);
        if (current.secondaryColor) this.secondaryColor.set(current.secondaryColor);
        if (current.logoUrl) this.logoPreviewUrl.set(current.logoUrl);
        if (current.logoUrlDark) this.logoDarkPreviewUrl.set(current.logoUrlDark);
      }
    }, { allowSignalWrites: true });
  }

  applyPalette(palette: ColorPalette) {
    this.primaryColor.set(palette.primary);
    this.secondaryColor.set(palette.secondary);
  }

  resetToDefault() {
    this.primaryColor.set('#6366F1');
    this.secondaryColor.set('#10B981');
  }

  onLogoSelect(event: any) {
    const files = event.files ?? event.currentFiles;
    if (files?.length > 0) {
      this.logoFile.set(files[0]);
      this.logoPreviewUrl.set(URL.createObjectURL(files[0]));
    }
  }

  onLogoDarkSelect(event: any) {
    const files = event.files ?? event.currentFiles;
    if (files?.length > 0) {
      this.logoFileDark.set(files[0]);
      this.logoDarkPreviewUrl.set(URL.createObjectURL(files[0]));
    }
  }

  onFaviconSelect(event: any) {
    const files = event.files ?? event.currentFiles;
    if (files?.length > 0) this.faviconFile.set(files[0]);
  }

  removeLogo() { this.logoFile.set(null); this.logoPreviewUrl.set(null); }
  removeLogoDark() { this.logoFileDark.set(null); this.logoDarkPreviewUrl.set(null); }
  removeFavicon() { this.faviconFile.set(null); }

  logoFileInput: HTMLInputElement | null = null;
  logoDarkFileInput: HTMLInputElement | null = null;
  faviconFileInput: HTMLInputElement | null = null;

  triggerLogoUpload() { document.getElementById('logoUploadInput')?.click(); }
  triggerLogoDarkUpload() { document.getElementById('logoDarkUploadInput')?.click(); }
  triggerFaviconUpload() { document.getElementById('faviconUploadInput')?.click(); }

  onLogoFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.logoFile.set(input.files[0]);
      this.logoPreviewUrl.set(URL.createObjectURL(input.files[0]));
    }
  }

  onLogoDarkFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.logoFileDark.set(input.files[0]);
      this.logoDarkPreviewUrl.set(URL.createObjectURL(input.files[0]));
    }
  }

  onFaviconFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.faviconFile.set(input.files[0]);
  }

  get userName(): string {
    return this.userService.currentUser()?.displayName || 'Sanjeev';
  }

  save() {
    this.isSaving.set(true);
    const formData = new FormData();
    formData.append('primaryColor', this.primaryColor());
    formData.append('secondaryColor', this.secondaryColor());
    if (this.logoFile()) formData.append('logoFile', this.logoFile() as Blob);
    if (this.logoFileDark()) formData.append('logoFileDark', this.logoFileDark() as Blob);
    if (this.faviconFile()) formData.append('faviconFile', this.faviconFile() as Blob);

    this.brandingService.updateBranding(formData).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Saved!', detail: 'Branding changes applied to all users.' });
        this.isSaving.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to save branding.' });
        this.isSaving.set(false);
      }
    });
  }
}
