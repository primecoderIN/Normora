import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/**
 * Service managing client-side dark mode theme preferences and SSR hydration safety.
 * Synchronizes user preference across localStorage, system media queries, and Tailwind/PrimeNG root classes.
 */
@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);
  isDarkMode = signal<boolean>(false);
  
  constructor() {
    this.initTheme();
  }

  /**
   * Initializes theme on application startup:
   * 1. Protects against SSR errors by verifying execution in browser environment.
   * 2. Checks localStorage for an explicit user choice ('dark' or 'light').
   * 3. Falls back to OS prefers-color-scheme media query if no explicit preference is stored.
   */
  private initTheme() {
    if (!this.isBrowser) return;

    const savedTheme = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    
    if (savedTheme === 'dark' || (!savedTheme && prefersDark)) {
      this.setDarkMode(true);
    } else {
      this.setDarkMode(false);
    }
  }

  toggleTheme() {
    this.setDarkMode(!this.isDarkMode());
  }

  /**
   * Updates reactive state, persists to localStorage, and updates DOM class list:
   * Adds 'dark' for Tailwind v4 and 'app-dark' / 'p-dark' for PrimeNG component dark palettes.
   */
  setDarkMode(isDark: boolean) {
    if (!this.isBrowser) return;

    this.isDarkMode.set(isDark);
    localStorage.setItem('theme', isDark ? 'dark' : 'light');

    const html = document.documentElement;
    if (isDark) {
      html.classList.add('app-dark', 'dark', 'p-dark');
    } else {
      html.classList.remove('app-dark', 'dark', 'p-dark');
    }
  }
}
