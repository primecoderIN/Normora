import { Component, input, output, signal, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { ActionCardDto } from '@core/services/conversation.service';

export interface FormFieldDefinition {
  fieldId: string;
  label: string;
  type: 'text' | 'number' | 'email' | 'select' | 'textarea';
  required?: boolean;
  placeholder?: string;
  options?: { label: string; value: string }[];
  defaultValue?: string;
}

/**
 * Interactive A2UI Dynamic Intake Form Card.
 * Renders dynamically configured form controls directly inside the conversation feed
 * when missing parameters are needed to execute a workspace action.
 */
@Component({
  selector: 'app-intake-form-card',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="p-3.5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 max-w-[95%] shadow-sm space-y-3">
      <!-- Card Header -->
      <div class="flex items-center justify-between gap-2">
        <div class="flex items-center gap-2 min-w-0">
          <div class="flex items-center justify-center w-6 h-6 rounded-lg bg-primary-100 dark:bg-primary-950/60 text-primary-600 dark:text-primary-400 shrink-0">
            <i class="pi pi-file-edit text-xs"></i>
          </div>
          <div>
            <h4 class="text-xs font-bold text-slate-900 dark:text-white m-0 truncate leading-tight">
              {{ card().title || 'Information Required' }}
            </h4>
            <p class="text-[10px] text-slate-500 dark:text-slate-400 m-0">Interactive parameter intake</p>
          </div>
        </div>
        <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[9px] font-bold uppercase tracking-wider bg-primary-50 dark:bg-primary-950/40 text-primary-600 dark:text-primary-400 border border-primary-200/60 dark:border-primary-800/60 shrink-0">
          Intake Form
        </span>
      </div>

      <!-- Context description -->
      @if (card().description) {
        <p class="text-[11px] text-slate-600 dark:text-slate-300 m-0 leading-relaxed">
          {{ card().description }}
        </p>
      }

      <!-- Form Body -->
      @if (card().isConfirmed) {
        <div class="p-3 rounded-xl bg-emerald-50 dark:bg-emerald-950/30 border border-emerald-200 dark:border-emerald-800 flex items-center gap-2 text-xs font-semibold text-emerald-700 dark:text-emerald-300">
          <i class="pi pi-check-circle text-sm"></i>
          <span>Information submitted successfully</span>
        </div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-2.5">
          @for (field of fields(); track field.fieldId) {
            <div>
              <label class="block text-[10px] font-semibold text-slate-600 dark:text-slate-300 uppercase tracking-wider mb-1">
                {{ field.label }}
                @if (field.required) {
                  <span class="text-red-500 ml-0.5">*</span>
                }
              </label>

              @switch (field.type) {
                @case ('select') {
                  <select
                    [formControlName]="field.fieldId"
                    class="w-full px-2.5 py-1.5 rounded-lg text-xs bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-primary-500"
                  >
                    @for (opt of field.options; track opt.value) {
                      <option [value]="opt.value">{{ opt.label }}</option>
                    }
                  </select>
                }
                @case ('textarea') {
                  <textarea
                    [formControlName]="field.fieldId"
                    [placeholder]="field.placeholder || ''"
                    rows="2"
                    class="w-full px-2.5 py-1.5 rounded-lg text-xs bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-primary-500 resize-none"
                  ></textarea>
                }
                @default {
                  <input
                    [type]="field.type"
                    [formControlName]="field.fieldId"
                    [placeholder]="field.placeholder || ''"
                    class="w-full px-2.5 py-1.5 rounded-lg text-xs bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-primary-500"
                  />
                }
              }
            </div>
          }

          <!-- Submit / Dismiss Buttons -->
          <div class="flex items-center gap-2 pt-1 border-t border-slate-100 dark:border-slate-800">
            <button
              type="submit"
              [disabled]="form.invalid || isSubmitting()"
              class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-primary-600 hover:bg-primary-500 text-white transition-all cursor-pointer shadow-2xs disabled:opacity-50 disabled:cursor-not-allowed"
            >
              @if (isSubmitting()) {
                <i class="pi pi-spin pi-spinner text-xs"></i>
              }
              <span>{{ card().confirmButtonText || 'Submit' }}</span>
            </button>

            @if (card().cancelButtonText) {
              <button
                type="button"
                (click)="onDismiss()"
                class="px-2.5 py-1.5 rounded-lg text-xs font-medium text-slate-500 hover:text-slate-700 dark:hover:text-slate-300 transition-all cursor-pointer"
              >
                {{ card().cancelButtonText }}
              </button>
            }
          </div>
        </form>
      }
    </div>
  `
})
export class IntakeFormCardComponent implements OnInit {
  readonly card = input.required<ActionCardDto>();
  readonly submitted = output<{ actionTarget: string; values: Record<string, any> }>();

  form = new FormGroup({});
  fields = signal<FormFieldDefinition[]>([]);
  isSubmitting = signal<boolean>(false);

  ngOnInit(): void {
    this.parseFields();
  }

  private parseFields(): void {
    const raw = this.card().parameters?.['fields'];
    if (!raw) return;

    try {
      const parsed: FormFieldDefinition[] = typeof raw === 'string' ? JSON.parse(raw) : raw;
      this.fields.set(parsed);

      for (const f of parsed) {
        const validators = f.required ? [Validators.required] : [];
        if (f.type === 'email') validators.push(Validators.email);
        this.form.addControl(f.fieldId, new FormControl(f.defaultValue || '', validators));
      }
    } catch {
      this.fields.set([]);
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.isSubmitting.set(true);
    const target = this.card().parameters?.['actionTarget'] || '';
    const values = this.form.value;

    this.card().isConfirmed = true;
    this.isSubmitting.set(false);
    this.submitted.emit({ actionTarget: target, values });
  }

  onDismiss(): void {
    this.card().isDismissed = true;
  }
}

