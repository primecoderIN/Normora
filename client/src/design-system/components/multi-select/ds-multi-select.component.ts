import {
  Component,
  input,
  output,
  signal,
  computed,
  forwardRef,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import {
  RdxFieldRoot,
  RdxFieldLabel,
  RdxFieldDescription,
  RdxFieldError,
} from '@radix-ng/primitives/field';
import {
  RdxSelectRoot,
  RdxSelectTrigger,
  RdxSelectPortal,
  RdxSelectPositioner,
  RdxSelectPopup,
  RdxSelectItem,
  RdxSelectItemText,
  RdxSelectItemIndicator,
} from '@radix-ng/primitives/select';

/**
 * Enterprise Design System MultiSelect component.
 * Built directly on top of @radix-ng/primitives/select [multiple]="true" and
 * @radix-ng/primitives/field headless primitives.
 * Replaces PrimeNG MultiSelect with an accessible multi-choice dropdown.
 * Displays selected items as chips, supports search filtering, and implements ControlValueAccessor.
 */
@Component({
  selector: 'ds-multi-select',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RdxFieldRoot,
    RdxFieldLabel,
    RdxFieldDescription,
    RdxFieldError,
    RdxSelectRoot,
    RdxSelectTrigger,
    RdxSelectPortal,
    RdxSelectPositioner,
    RdxSelectPopup,
    RdxSelectItem,
    RdxSelectItemText,
    RdxSelectItemIndicator,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DsMultiSelectComponent),
      multi: true,
    },
  ],
  template: `
    <div
      rdxFieldRoot
      [invalid]="isInvalid()"
      [disabled]="isDisabled()"
      [required]="required()"
      class="w-full flex flex-col gap-1.5"
      [class]="containerClass()"
    >
      @if (label()) {
        <label
          rdxFieldLabel
          class="text-xs font-semibold uppercase tracking-wider text-slate-700 dark:text-slate-300 select-none flex items-center justify-between"
        >
          <span>
            {{ label() }}
            @if (required()) {
              <span class="text-red-500 font-bold ml-0.5">*</span>
            }
          </span>
          @if (hint() && !errorMessage()) {
            <span
              rdxFieldDescription
              class="text-[11px] font-normal text-slate-400 dark:text-slate-500 lowercase"
            >
              {{ hint() }}
            </span>
          }
        </label>
      }

      <div
        rdxSelectRoot
        [multiple]="true"
        [value]="selectedValues()"
        [disabled]="isDisabled()"
        (valueChange)="onRadixValueChange($event)"
      >
        <!-- MultiSelect Trigger with Selected Chips -->
        <button
          rdxSelectTrigger
          type="button"
          [class]="computedTriggerClasses()"
        >
          <div class="flex flex-wrap items-center gap-1.5 flex-1 min-w-0 py-1">
            @if (selectedItems().length === 0) {
              <span class="text-slate-400 dark:text-slate-500 text-sm font-normal">
                {{ placeholder() || 'Select items...' }}
              </span>
            } @else {
              @for (item of selectedItems(); track getOptionValue(item)) {
                <span
                  class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-medium bg-indigo-50 dark:bg-indigo-950/60 text-indigo-700 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-800 shrink-0"
                >
                  <span class="truncate max-w-30">{{ getOptionLabel(item) }}</span>
                  <span
                    role="button"
                    tabindex="0"
                    (click)="removeItem(item, $event)"
                    class="hover:text-red-500 transition-colors focus:outline-none cursor-pointer"
                    aria-label="Remove item"
                  >
                    <i class="pi pi-times text-[10px]" aria-hidden="true"></i>
                  </span>
                </span>
              }
            }
          </div>

          <span class="shrink-0 text-slate-400 dark:text-slate-500 ml-2">
            <i class="pi pi-chevron-down text-xs" aria-hidden="true"></i>
          </span>
        </button>

        <!-- Portaled and Floating-UI Positioned Dropdown Popup -->
        <ng-template rdxSelectPortal>
          <div
            rdxSelectPositioner
            side="bottom"
            align="start"
            [sideOffset]="4"
            class="z-1000 invisible data-[side]:visible"
          >
            <div
              rdxSelectPopup
              class="w-full min-w-50 max-h-64 overflow-y-auto custom-scrollbar rounded-(--ds-control-radius) bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl py-1.5 data-[side]:animate-in data-[side]:fade-in data-[side]:zoom-in-95 duration-150 select-none"
            >
              <!-- Search Header -->
              @if (options().length > 5) {
                <div class="px-2.5 pb-2 mb-1 border-b border-slate-100 dark:border-slate-800">
                  <input
                    type="text"
                    [value]="searchQuery()"
                    (input)="onSearchInput($event)"
                    (click)="$event.stopPropagation()"
                    (keydown)="$event.stopPropagation()"
                    placeholder="Search..."
                    class="w-full text-xs px-2.5 py-1.5 rounded border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-(--ds-color-primary)"
                  />
                </div>
              }

              <!-- Options List -->
              @if (filteredOptions().length === 0) {
                <div class="px-3.5 py-2 text-xs text-slate-400 dark:text-slate-500 italic">
                  No matching options
                </div>
              } @else {
                @for (opt of filteredOptions(); track getOptionValue(opt)) {
                  <div
                    rdxSelectItem
                    [value]="getOptionValue(opt)"
                    class="px-3.5 py-2 text-sm flex items-center justify-between cursor-pointer transition-colors duration-100 select-none text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 data-highlighted:bg-primary-50 dark:data-highlighted:bg-primary-950/60 data-highlighted:text-primary-700 dark:data-highlighted:text-primary-300 data-[state=checked]:bg-primary-50/50 dark:data-[state=checked]:bg-primary-950/30 focus:outline-none"
                  >
                    <div class="flex items-center gap-2.5 truncate flex-1">
                      <div
                        class="w-4 h-4 rounded border flex items-center justify-center transition-colors shrink-0"
                        [class]="isItemSelected(opt) ? 'bg-(--primary) border-(--primary) text-white' : 'border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800'"
                      >
                        <span rdxSelectItemIndicator class="text-white flex items-center justify-center">
                          <i class="pi pi-check text-[10px]" aria-hidden="true"></i>
                        </span>
                      </div>
                      <span rdxSelectItemText class="truncate">
                        {{ getOptionLabel(opt) }}
                      </span>
                    </div>
                  </div>
                }
              }
            </div>
          </div>
        </ng-template>
      </div>

      @if (errorMessage() && isInvalid()) {
        <p
          rdxFieldError
          class="text-xs text-red-600 dark:text-red-400 font-medium flex items-center gap-1 mt-0.5"
          role="alert"
        >
          <i class="pi pi-exclamation-circle text-xs shrink-0" aria-hidden="true"></i>
          <span>{{ errorMessage() }}</span>
        </p>
      }
    </div>
  `,
})
export class DsMultiSelectComponent implements ControlValueAccessor {
  readonly options = input<any[]>([]);
  readonly optionLabel = input<string | undefined>(undefined);
  readonly optionValue = input<string | undefined>(undefined);
  readonly placeholder = input<string>('Select items...');
  readonly label = input<string | undefined>(undefined);
  readonly required = input<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly errorMessage = input<string | undefined>(undefined);
  readonly hint = input<string | undefined>(undefined);
  readonly styleClass = input<string>('');
  readonly containerClass = input<string>('');

  readonly change = output<any[]>();
  readonly onChange = output<any>(); // PrimeNG compatibility alias

  protected readonly selectedValues = signal<any[]>([]);
  protected readonly searchQuery = signal<string>('');
  private readonly formDisabled = signal<boolean>(false);

  protected readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
  protected readonly isInvalid = computed(() => this.invalid() || !!this.errorMessage());

  protected readonly filteredOptions = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    if (!q) return this.options();
    return this.options().filter((opt) =>
      this.getOptionLabel(opt).toLowerCase().includes(q)
    );
  });

  protected readonly selectedItems = computed(() => {
    const vals = this.selectedValues();
    const opts = this.options();
    const optValueKey = this.optionValue();

    if (!optValueKey) {
      return vals;
    }

    return opts.filter((opt) => {
      const v = this.getOptionValue(opt);
      return vals.some((selected) => this.areEqual(selected, v));
    });
  });

  protected readonly computedTriggerClasses = computed(() => {
    const classes = [
      'w-full min-h-(--ds-control-height-md) px-3 flex items-center justify-between text-left select-none',
      'rounded-(--radius) border transition-all duration-150',
      'bg-(--background) text-(--foreground)',
      'border-(--input) hover:border-(--ds-input-border-hover)',
      'focus:outline-none focus:border-(--primary) focus:ring-2 focus:ring-(--ring)',
      'data-[state=open]:border-(--primary) data-[state=open]:ring-2 data-[state=open]:ring-(--ring)',
      'data-invalid:border-(--destructive) focus:data-invalid:border-(--destructive) focus:ring-2 focus:ring-red-500/20 dark:focus:ring-red-400/25',
      'disabled:bg-(--muted) disabled:text-(--muted-foreground) disabled:cursor-not-allowed',
      'data-disabled:bg-(--muted) data-disabled:text-(--muted-foreground) data-disabled:cursor-not-allowed',
      'cursor-pointer',
    ];

    if (this.isInvalid()) {
      classes.push(
        'border-(--destructive) focus:border-(--destructive)',
        'focus:ring-2 focus:ring-red-500/20 dark:focus:ring-red-400/25'
      );
    }

    if (this.styleClass()) {
      classes.push(this.styleClass());
    }

    return classes.join(' ');
  });

  protected getOptionLabel(option: any): string {
    if (option === null || option === undefined) return '';
    const key = this.optionLabel();
    if (key && typeof option === 'object') {
      return String(option[key] ?? '');
    }
    return String(option);
  }

  protected getOptionValue(option: any): any {
    if (option === null || option === undefined) return option;
    const key = this.optionValue();
    if (key && typeof option === 'object') {
      return option[key];
    }
    return option;
  }

  protected isItemSelected(option: any): boolean {
    const targetVal = this.getOptionValue(option);
    return this.selectedValues().some((v) => this.areEqual(v, targetVal));
  }

  private areEqual(a: any, b: any): boolean {
    if (a === b) return true;
    if (a && b && typeof a === 'object' && typeof b === 'object') {
      if (a.id && b.id) return a.id === b.id;
    }
    return false;
  }

  protected onSearchInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.searchQuery.set(val);
  }

  protected onRadixValueChange(val: any): void {
    const arr = Array.isArray(val) ? val : (val != null ? [val] : []);
    this.selectedValues.set(arr);
    this.onChangeCallback(arr);
    this.onTouchedCallback();

    const payload = { value: arr, originalEvent: null };
    this.change.emit(arr);
    this.onChange.emit(payload);
  }

  removeItem(item: any, event: MouseEvent): void {
    event.stopPropagation();
    event.preventDefault();
    const itemVal = this.getOptionValue(item);
    const updated = this.selectedValues().filter((v) => !this.areEqual(v, itemVal));

    this.selectedValues.set(updated);
    this.onChangeCallback(updated);
    this.onTouchedCallback();

    const payload = { value: updated, originalEvent: event };
    this.change.emit(updated);
    this.onChange.emit(payload);
  }

  // ControlValueAccessor
  private onChangeCallback: (val: any) => void = () => {};
  private onTouchedCallback: () => void = () => {};

  writeValue(val: any): void {
    if (Array.isArray(val)) {
      this.selectedValues.set(val);
    } else {
      this.selectedValues.set([]);
    }
  }

  registerOnChange(fn: (val: any) => void): void {
    this.onChangeCallback = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouchedCallback = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.formDisabled.set(isDisabled);
  }
}
