import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
import { catchError, of, tap } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';

export interface TenantBrandingDto {
  tenantId: string;
  tenantName: string;
  primaryColor: string | null;
  secondaryColor: string | null;
  logoUrl: string | null;
  logoUrlDark: string | null;
  faviconUrl: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class TenantBrandingService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/tenants`;

  /**
   * The currently applied branding configuration, exposed as a signal for reactive UI bindings (like logos).
   */
  currentBranding = signal<TenantBrandingDto | null>(null);

  // Subdomain routing logic has been removed in favor of path-based workspace routing


  /**
   * Fetches branding for the given slug and applies CSS variables globally.
   */
  applyBrandingForSlug(slug: string) {
    return this.http.get<ApiResponse<TenantBrandingDto>>(`${this.apiUrl}/branding/${slug}`).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.currentBranding.set(response.data);
          this.applyCssVariables(response.data);
        }
      }),
      catchError(() => of(null))
    );
  }

  /**
   * Updates the active tenant's branding configuration.
   */
  updateBranding(payload: FormData) {
    return this.http.put<ApiResponse<TenantBrandingDto>>(`${this.apiUrl}/branding`, payload).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.currentBranding.set(response.data);
          this.applyCssVariables(response.data);
        }
      })
    );
  }

  /**
   * Fetches branding for the currently authenticated tenant by slug.
   * Called after login to ensure authenticated users see their brand colors.
   */
  applyBrandingForAuthenticatedTenant(tenantSlug: string) {
    return this.applyBrandingForSlug(tenantSlug);
  }

  /**
   * Injects the tenant's brand colors into the document root as CSS custom properties.
   * Generates the full PrimeNG Aura primary palette (50-950) from a single hex value
   * so all PrimeNG components (buttons, inputs, focus rings) adopt the brand color.
   */
  private applyCssVariables(branding: TenantBrandingDto): void {
    if (typeof document === 'undefined') return;

    const root = document.documentElement;

    if (branding.primaryColor) {
      root.style.setProperty('--brand-primary', branding.primaryColor);

      // Generate a full palette from the hex color
      const palette = this.generatePalette(branding.primaryColor);

      // Set all PrimeNG Aura primary CSS variables
      root.style.setProperty('--p-primary-50',  palette[50]);
      root.style.setProperty('--p-primary-100', palette[100]);
      root.style.setProperty('--p-primary-200', palette[200]);
      root.style.setProperty('--p-primary-300', palette[300]);
      root.style.setProperty('--p-primary-400', palette[400]);
      root.style.setProperty('--p-primary-500', palette[500]);
      root.style.setProperty('--p-primary-600', palette[600]);
      root.style.setProperty('--p-primary-700', palette[700]);
      root.style.setProperty('--p-primary-800', palette[800]);
      root.style.setProperty('--p-primary-900', palette[900]);
      root.style.setProperty('--p-primary-950', palette[950]);

      // Also set the legacy variable for any custom usages
      root.style.setProperty('--p-primary-color', branding.primaryColor);
    }

    if (branding.secondaryColor) {
      root.style.setProperty('--brand-secondary', branding.secondaryColor);
    }

    if (branding.faviconUrl) {
      const favicon = document.querySelector<HTMLLinkElement>('link[rel="icon"]');
      if (favicon) favicon.href = branding.faviconUrl;
    }
  }

  /**
   * Generates a Tailwind-style color palette (50–950) from a base hex color.
   * The base color maps to the 500 shade, and lighter/darker variants are generated
   * by interpolating with white and black respectively.
   */
  private generatePalette(hex: string): Record<number, string> {
    const base = this.hexToRgb(hex);
    if (!base) return {};

    const white = { r: 255, g: 255, b: 255 };
    const black = { r: 0, g: 0, b: 0 };

    const shades: Record<number, number> = {
      50: 0.95, 100: 0.9, 200: 0.75, 300: 0.6, 400: 0.3,
      500: 0,
      600: 0.15, 700: 0.3, 800: 0.5, 900: 0.65, 950: 0.8,
    };

    const palette: Record<number, string> = {};
    for (const [shade, mix] of Object.entries(shades)) {
      const num = Number(shade);
      const target = num <= 500 ? white : black;
      const factor = num <= 500 ? mix : mix;
      const r = Math.round(base.r + (target.r - base.r) * factor);
      const g = Math.round(base.g + (target.g - base.g) * factor);
      const b = Math.round(base.b + (target.b - base.b) * factor);
      palette[num] = `rgb(${r}, ${g}, ${b})`;
    }
    return palette;
  }

  private hexToRgb(hex: string): { r: number; g: number; b: number } | null {
    const clean = hex.replace('#', '');
    if (clean.length !== 6) return null;
    return {
      r: parseInt(clean.substring(0, 2), 16),
      g: parseInt(clean.substring(2, 4), 16),
      b: parseInt(clean.substring(4, 6), 16),
    };
  }

}
