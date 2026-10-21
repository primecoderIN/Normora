import { Component, input, output, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DsButtonComponent, DsDialogComponent } from '@design-system';
import { Department } from '@core/services/department.service';
import { FormErrorComponent } from '@shared/components/form-error/form-error.component';

/**
 * Modal form component for creating or editing department metadata.
 */
@Component({
  selector: 'app-department-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DsButtonComponent, DsDialogComponent, FormErrorComponent],
  template: `
    <ds-dialog
      [visible]="true"
      (visibleChange)="onCancel()"
      [title]="department() ? 'Edit department' : 'New department'"
      maxWidthClass="max-w-md"
    >
      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
        <label class="flex flex-col gap-1.5">
          <span class="text-sm font-semibold text-slate-700 dark:text-slate-200">Name <span class="text-red-500">*</span></span>
          <input
            type="text"
            formControlName="name"
            class="w-full h-10 px-3.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-slate-500 text-sm outline-none! focus:outline-none! focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 transition-all"
            placeholder="e.g. Engineering"
            id="dept-name"
          />
          @if (form.get('name')?.invalid && form.get('name')?.touched) {
            <span class="text-xs font-medium text-red-500 mt-1">Department name is required.</span>
          }
        </label>
        <label class="flex flex-col gap-1.5">
          <span class="text-sm font-semibold text-slate-700 dark:text-slate-200">Description</span>
          <textarea
            formControlName="description"
            class="w-full p-3.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-slate-500 text-sm outline-none! focus:outline-none! resize-none focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 transition-all custom-scrollbar"
            rows="3"
            placeholder="Optional description…"
            id="dept-description"
          ></textarea>
        </label>
        
        <app-form-error [error]="error()"></app-form-error>

        <div class="flex items-center justify-end gap-3 mt-4 pt-4 border-t border-slate-100 dark:border-slate-800">
          <ds-button variant="outline" size="sm" (clicked)="onCancel()">Cancel</ds-button>
          <ds-button
            variant="primary"
            size="sm"
            type="submit"
            [disabled]="form.invalid || isSaving()"
            [loading]="isSaving()"
          >
            {{ department() ? 'Save changes' : 'Create department' }}
          </ds-button>
        </div>
      </form>
    </ds-dialog>
  `
})
export class DepartmentFormComponent implements OnInit {
  private fb = inject(FormBuilder);

  department = input<Department | null>(null);
  isSaving = input<boolean>(false);
  error = input<string | null | undefined>('');
  
  save = output<{ name: string; description: string }>();
  cancel = output<void>();

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
  });

  ngOnInit() {
    const dept = this.department();
    if (dept) {
      this.form.patchValue({ name: dept.name, description: dept.description ?? '' });
    }
  }

  onSubmit() {
    if (this.form.invalid) return;
    this.save.emit(this.form.value);
  }

  onCancel() {
    this.cancel.emit();
  }
}
