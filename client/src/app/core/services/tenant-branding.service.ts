import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { catchError, of, tap } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';

export interface TenantBrandingDto {
  tenantId: string;
  tenantName: string;
  primaryColor: string | null;
  secondaryColor: string | null;
  logoUrl: string | null;
  faviconUrl: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class TenantBrandingService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/tenants`;

  /**
   * Extracts the tenant slug from the current subdomain.
   * e.g. "intel.localhost" → "intel", "localhost" → null
   */
  getSlugFromSubdomain(): string | null {
    const hostname = window.location.hostname;
    const parts = hostname.split('.');
    // Subdomain exists if there are at least 2 parts and it's not just "www"
    if (parts.length >= 2 && parts[0] !== 'www') {
      return parts[0];
    }
    return null;
  }

  /**
   * Fetches branding for the given slug and applies CSS variables globally.
   */
  applyBrandingForSlug(slug: string) {
    return this.http.get<ApiResponse<TenantBrandingDto>>(`${this.apiUrl}/branding/${slug}`).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.applyCssVariables(response.data);
        }
      }),
      catchError(() => of(null))
    );
  }

  /**
   * Injects the tenant's brand colors into the document root as CSS custom properties.
   * This causes the entire app to reskin automatically.
   */
  private applyCssVariables(branding: TenantBrandingDto): void {
    const root = document.documentElement;

    if (branding.primaryColor) {
      root.style.setProperty('--brand-primary', branding.primaryColor);
    }
    if (branding.secondaryColor) {
      root.style.setProperty('--brand-secondary', branding.secondaryColor);
    }
    if (branding.faviconUrl) {
      const favicon = document.querySelector<HTMLLinkElement>('link[rel="icon"]');
      if (favicon) {
        favicon.href = branding.faviconUrl;
      }
    }
  }

}
