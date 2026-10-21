import { Component, input, output, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  DsButtonComponent,
  DsDialogComponent,
  DsInputComponent,
  DsTextareaComponent,
} from '@design-system';
import { Department } from '@core/services/department.service';
import { FormErrorComponent } from '@shared/components/form-error/form-error.component';

/**
 * Modal form component for creating or editing department metadata.
 */
@Component({
  selector: 'app-department-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DsButtonComponent,
    DsDialogComponent,
    DsInputComponent,
    DsTextareaComponent,
    FormErrorComponent,
  ],
  template: `
    <ds-dialog
      [visible]="true"
      (visibleChange)="onCancel()"
      [title]="department() ? 'Edit department' : 'New department'"
      maxWidthClass="max-w-md"
    >
      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
        <ds-input
          label="Name"
          [required]="true"
          formControlName="name"
          placeholder="e.g. Engineering"
          id="dept-name"
          [errorMessage]="form.get('name')?.invalid && form.get('name')?.touched ? 'Department name is required.' : undefined"
        />

        <ds-textarea
          label="Description"
          formControlName="description"
          placeholder="Optional description…"
          id="dept-description"
          [rows]="3"
        />
        
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
