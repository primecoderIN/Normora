import { Component, inject, signal, effect, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TenantBrandingService } from '@core/services/tenant-branding.service';
import { UserService } from '@core/services/user.service';
import { DashboardService } from '@core/services/dashboard.service';
import { ApiMessages } from '@core/constants/api-messages';
import {
  DsButtonComponent,
  DsColorPickerComponent,
  DsToastComponent,
  DsToastService,
} from '@design-system';

export interface ColorPalette {
  name: string;
  primary: string;
  secondary: string;
}

/**
 * White-label employer branding settings component.
 * Allows administrators to customize theme colors, upload light/dark logos, and configure favicons.
 */
@Component({
  selector: 'app-employer-branding-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DsButtonComponent,
    DsColorPickerComponent,
    DsToastComponent,
  ],
  templateUrl: './branding.component.html',
})
export class BrandingComponent implements OnInit {
  private brandingService = inject(TenantBrandingService);
  private dashboardService = inject(DashboardService);
  private messageService = inject(DsToastService);
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

  stats = signal([
    { label: 'Documents', value: '12', delta: '+12%' },
    { label: 'Conversations', value: '28', delta: '+8%' },
    { label: 'Employees', value: '6', delta: '+4%' }
  ]);

  recentActivity = signal([
    { icon: 'pi-file-pdf', label: 'Company Handbook.pdf', sub: 'Uploaded recently' },
    { icon: 'pi-comments', label: 'Benefits & Leave Discussion', sub: 'Active inquiry' },
    { icon: 'pi-user-plus', label: 'New employee joined', sub: 'Workspace member' }
  ]);

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

  onLogoSelect(event: { files?: File[]; currentFiles?: File[] }) {
    const files = event.files ?? event.currentFiles;
    if (files && files.length > 0) {
      this.logoFile.set(files[0]);
      this.logoPreviewUrl.set(URL.createObjectURL(files[0]));
    }
  }

  onLogoDarkSelect(event: { files?: File[]; currentFiles?: File[] }) {
    const files = event.files ?? event.currentFiles;
    if (files && files.length > 0) {
      this.logoFileDark.set(files[0]);
      this.logoDarkPreviewUrl.set(URL.createObjectURL(files[0]));
    }
  }

  onFaviconSelect(event: { files?: File[]; currentFiles?: File[] }) {
    const files = event.files ?? event.currentFiles;
    if (files && files.length > 0) this.faviconFile.set(files[0]);
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

  ngOnInit(): void {
    this.dashboardService.getSummary().subscribe({
      next: (res) => {
        if (res.data?.stats) {
          const s = res.data.stats;
          this.stats.set([
            { label: 'Documents', value: s.totalDocuments.toString(), delta: `${s.documentsTrend >= 0 ? '+' : ''}${s.documentsTrend}%` },
            { label: 'Conversations', value: s.totalQuestions.toString(), delta: `${s.questionsTrend >= 0 ? '+' : ''}${s.questionsTrend}%` },
            { label: 'Employees', value: s.totalEmployees.toString(), delta: `${s.employeesTrend >= 0 ? '+' : ''}${s.employeesTrend}%` }
          ]);
        }
        if (res.data?.recentDocuments && res.data.recentDocuments.length > 0) {
          this.recentActivity.set(res.data.recentDocuments.slice(0, 3).map(d => ({
            icon: d.type === 'pdf' ? 'pi-file-pdf' : 'pi-file',
            label: d.name,
            sub: `${d.category} · ${d.status}`
          })));
        }
      },
      error: () => {}
    });
  }

  get userName(): string {
    return this.userService.currentUser()?.displayName || 'Workspace Admin';
  }

  get userInitials(): string {
    const name = this.userService.currentUser()?.displayName?.trim() || 'Admin';
    const parts = name.split(' ').filter(Boolean);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }

  save() {
    // White-Label Multipart Form Serialization:
    // Bundles custom primary/secondary HEX color codes with binary image assets (light logo, dark logo, favicon).
    // The backend stores binaries in MinIO under '{tenantId}/branding/' and returns cache-busted asset URLs.
    // TenantBrandingService updates CSS custom properties (--color-primary, --color-secondary) dynamically
    // on document.documentElement so the entire UI theme repaints instantly.
    this.isSaving.set(true);
    const formData = new FormData();
    formData.append('primaryColor', this.primaryColor());
    formData.append('secondaryColor', this.secondaryColor());
    if (this.logoFile()) formData.append('logoFile', this.logoFile() as Blob);
    if (this.logoFileDark()) formData.append('logoFileDark', this.logoFileDark() as Blob);
    if (this.faviconFile()) formData.append('faviconFile', this.faviconFile() as Blob);

    this.brandingService.updateBranding(formData).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: ApiMessages.BrandingSaved.summary, detail: ApiMessages.BrandingSaved.detail });
        this.isSaving.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: ApiMessages.BrandingSaveFailed.summary, detail: ApiMessages.BrandingSaveFailed.detail });
        this.isSaving.set(false);
      }
    });
  }
}
