import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  isDarkMode = signal<boolean>(false);
  
  constructor() {
    this.initTheme();
  }

  private initTheme() {
    // Only run in browser (not SSR)
    if (typeof window === 'undefined') return;

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
    if (typeof window === 'undefined') return;

    this.isDarkMode.set(isDark);
    localStorage.setItem('theme', isDark ? 'dark' : 'light');

    const html = document.documentElement;
    if (isDark) {
      html.classList.add('app-dark');
      html.classList.add('p-dark'); // PrimeNG v18+ default dark mode class
    } else {
      html.classList.remove('app-dark');
      html.classList.remove('p-dark');
    }
  }
}
