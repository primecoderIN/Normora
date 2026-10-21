import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DsButtonComponent } from '@design-system';
import { AppRoutes } from '@core/constants/app-routes';

/**
 * 404 Not Found error view shown for unmapped or unauthorized resource routes.
 */
@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink, DsButtonComponent],
  template: `

    <div class="min-h-screen bg-white dark:bg-slate-900 flex flex-col items-center justify-center p-4 relative overflow-hidden transition-colors duration-200">
      
      <!-- Decorative Background Elements -->
      <div class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-200 h-200 bg-indigo-500/10 dark:bg-indigo-500/5 rounded-full blur-[100px] -z-10 pointer-events-none"></div>
      
      <div class="text-center z-10 max-w-lg mx-auto">
        <!-- 404 Number -->
        <h1 class="text-9xl font-extrabold text-transparent bg-clip-text bg-linear-to-r from-indigo-600 to-purple-600 dark:from-indigo-400 dark:to-purple-400 mb-4 animate-pulse">
          404
        </h1>
        
        <!-- Heading -->
        <h2 class="text-3xl md:text-4xl font-bold text-slate-800 dark:text-slate-100 mb-4 tracking-tight">
          Page Not Found
        </h2>
        
        <!-- Message -->
        <p class="text-lg text-slate-500 dark:text-slate-400 mb-10">
          Sorry, we couldn't find the page you're looking for. It might have been moved, deleted, or perhaps it never existed.
        </p>
        
        <!-- Action Button -->
        <a [routerLink]="AppRoutes.Login" class="inline-block">
          <ds-button
            variant="primary"
            size="lg"
            icon="pi pi-home"
            styleClass="shadow-lg hover:shadow-xl transition-shadow"
          >
            Return to Safety
          </ds-button>
        </a>
      </div>
      
      <!-- Bottom Watermark/Logo -->
      <div class="absolute bottom-8 text-slate-400 dark:text-slate-600 font-semibold tracking-widest uppercase text-sm">
        Normora
      </div>
    </div>
  `
})
export class NotFoundComponent {
  AppRoutes = AppRoutes;
}
