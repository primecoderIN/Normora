import { Component, input } from '@angular/core';

@Component({
  selector: 'app-form-error',
  standalone: true,
  template: `
    @if (error()) {
      <div class="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-red-700 mt-2">
        <i class="pi pi-exclamation-circle mt-0.5"></i>
        <span>{{ error() }}</span>
      </div>
    }
  `
})
export class FormErrorComponent {
  error = input<string | null | undefined>('');
}
