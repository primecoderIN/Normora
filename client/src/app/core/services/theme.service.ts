import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

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
