import { Component, input } from '@angular/core';

@Component({
  selector: 'app-form-error',
  standalone: true,
  template: `
    @if (error()) {
      <div
        class="flex items-start gap-2.5 p-3 mt-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800 rounded-lg text-sm text-red-700 dark:text-red-400"
        role="alert"
      >
        <i class="pi pi-exclamation-circle mt-0.5 flex-none text-base"></i>
        <span class="leading-snug">{{ error() }}</span>
      </div>
    }
  `
})
export class FormErrorComponent {
  error = input<string | null | undefined>('');
}
