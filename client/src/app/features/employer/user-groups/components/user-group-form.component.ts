import { Component, input, output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { UserGroup } from '../../../../core/services/user-group.service';
import { FormErrorComponent } from '../../../../shared/components/form-error/form-error.component';

@Component({
  selector: 'app-user-group-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ButtonModule, FormErrorComponent],
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-surface-900/40 backdrop-blur-sm" (click)="onCancel()">
      <div class="w-full max-w-md bg-white rounded-xl shadow-2xl overflow-hidden flex flex-col" (click)="$event.stopPropagation()">
        <div class="flex items-center justify-between p-5 border-b border-surface-100">
          <h3 class="text-lg font-bold text-surface-900 m-0">{{ userGroup() ? 'Edit group' : 'New user group' }}</h3>
          <p-button icon="pi pi-times" (onClick)="onCancel()" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-surface-100 hover:!text-surface-700 rounded-full transition-colors"></p-button>
        </div>
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4 p-5">
          <label class="flex flex-col gap-1.5">
            <span class="text-sm font-semibold text-surface-700">Name <span class="text-red-500">*</span></span>
            <input
              type="text"
              formControlName="name"
              class="w-full h-10 px-3 bg-white border border-surface-300 rounded-md text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all"
              placeholder="e.g. Engineering Team"
              id="group-name"
            />
            @if (form.get('name')?.invalid && form.get('name')?.touched) {
              <span class="text-xs font-medium text-red-500 mt-1">Group name is required.</span>
            }
          </label>
          <label class="flex flex-col gap-1.5">
            <span class="text-sm font-semibold text-surface-700">Description</span>
            <textarea
              formControlName="description"
              class="w-full p-3 bg-white border border-surface-300 rounded-md text-sm outline-none resize-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all custom-scrollbar"
              rows="3"
              placeholder="Optional description…"
              id="group-description"
            ></textarea>
          </label>
          
          <app-form-error [error]="error()"></app-form-error>

          <div class="flex items-center justify-end gap-3 mt-4 pt-4 border-t border-surface-100">
            <p-button label="Cancel" (onClick)="onCancel()" styleClass="!bg-white !border-surface-300 !text-surface-700 hover:!bg-surface-50 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
            <p-button [label]="userGroup() ? 'Save changes' : 'Create group'" type="submit" [disabled]="form.invalid || isSaving()" [icon]="isSaving() ? 'pi pi-spin pi-spinner' : ''" styleClass="!bg-indigo-600 !border-indigo-600 !text-white hover:!bg-indigo-700 disabled:!bg-surface-300 disabled:!border-surface-300 disabled:!text-surface-500 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class UserGroupFormComponent implements OnInit {
  userGroup = input<UserGroup | null>(null);
  isSaving = input<boolean>(false);
  error = input<string | null | undefined>('');
  
  save = output<{ name: string; description: string }>();
  cancel = output<void>();

  form: FormGroup;

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: [''],
    });
  }

  ngOnInit() {
    const group = this.userGroup();
    if (group) {
      this.form.patchValue({ name: group.name, description: group.description ?? '' });
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
