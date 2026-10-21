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
  RdxSelectValue,
  RdxSelectPortal,
  RdxSelectPositioner,
  RdxSelectPopup,
  RdxSelectItem,
  RdxSelectItemText,
  RdxSelectItemIndicator,
} from '@radix-ng/primitives/select';

/**
 * Enterprise Design System Select / Dropdown component.
 * Built directly on top of @radix-ng/primitives/select and @radix-ng/primitives/field headless primitives.
 * Provides accessible keyboard navigation (arrow keys, enter, escape, typeahead),
 * viewport positioning, ControlValueAccessor integration, and centralized border tokens.
 */
@Component({
  selector: 'ds-select',
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
    RdxSelectValue,
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
      useExisting: forwardRef(() => DsSelectComponent),
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
              <span class="text-red-500 font-bold ml-0.5" aria-hidden="true">*</span>
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
        [value]="selectedValue()"
        [disabled]="isDisabled()"
        (valueChange)="onRadixValueChange($event)"
      >
        <!-- Radix Select Trigger -->
        <button
          rdxSelectTrigger
          type="button"
          [class]="computedTriggerClasses()"
        >
          <span
            rdxSelectValue
            [placeholder]="placeholder() || 'Select an option'"
            class="truncate font-normal"
            [class.text-slate-400]="!selectedLabel()"
            [class.dark:text-slate-500]="!selectedLabel()"
          >
            {{ selectedLabel() || placeholder() || 'Select an option' }}
          </span>

          <div class="flex items-center gap-1.5 shrink-0 ml-2">
            @if (showClear() && selectedValue() !== undefined && selectedValue() !== null && selectedValue() !== '') {
              <span
                role="button"
                tabindex="0"
                (click)="clearSelection($event)"
                class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 cursor-pointer p-0.5"
                aria-label="Clear selection"
              >
                <i class="pi pi-times text-xs" aria-hidden="true"></i>
              </span>
            }
            <span class="pointer-events-none text-slate-400 dark:text-slate-500">
              <i class="pi pi-chevron-down text-xs" aria-hidden="true"></i>
            </span>
          </div>
        </button>

        <!-- Radix Select Content (Portaled & Positioned) -->
        <ng-template rdxSelectPortal>
          <div
            rdxSelectPositioner
            side="bottom"
            align="start"
            [sideOffset]="4"
            class="z-dropdown invisible data-side:visible"
          >
            <div
              rdxSelectPopup
              class="w-full min-w-(--radix-popper-anchor-width,11.25rem) max-h-60 overflow-y-auto custom-scrollbar rounded-(--ds-control-radius) bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl py-1 select-none"
            >
              @if (options().length === 0) {
                <div class="px-3.5 py-2.5 text-xs text-slate-400 dark:text-slate-500 italic">
                  No options available
                </div>
              } @else {
                @for (opt of options(); track getOptionValue(opt)) {
                  <div
                    rdxSelectItem
                    [value]="getOptionValue(opt)"
                    class="px-3.5 py-2 text-sm flex items-center justify-between cursor-pointer transition-colors duration-100 select-none text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 data-highlighted:bg-primary-50 dark:data-highlighted:bg-primary-950/60 data-highlighted:text-primary-700 dark:data-highlighted:text-primary-300 data-[state=checked]:font-semibold focus:outline-none"
                  >
                    <span rdxSelectItemText class="truncate">
                      {{ getOptionLabel(opt) }}
                    </span>

                    <span rdxSelectItemIndicator class="text-(--primary) shrink-0 ml-2">
                      <i class="pi pi-check text-xs" aria-hidden="true"></i>
                    </span>
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
export class DsSelectComponent implements ControlValueAccessor {
  readonly options = input<any[]>([]);
  readonly optionLabel = input<string | undefined>(undefined);
  readonly optionValue = input<string | undefined>(undefined);
  readonly placeholder = input<string>('Select an option');
  readonly label = input<string | undefined>(undefined);
  readonly required = input<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly errorMessage = input<string | undefined>(undefined);
  readonly hint = input<string | undefined>(undefined);
  readonly showClear = input<boolean>(false);
  readonly styleClass = input<string>('');
  readonly containerClass = input<string>('');

  readonly change = output<any>();
  readonly onChange = output<any>(); // PrimeNG compatibility alias

  protected readonly selectedValue = signal<any>(undefined);
  private readonly formDisabled = signal<boolean>(false);

  clearSelection(event: MouseEvent): void {
    event.stopPropagation();
    event.preventDefault();
    this.selectedValue.set(null);
    this.onChangeCallback(null);
    this.onTouchedCallback();

    const eventPayload = { value: null, originalEvent: event };
    this.change.emit(eventPayload);
    this.onChange.emit(eventPayload);
  }

  protected readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
  protected readonly isInvalid = computed(() => this.invalid() || !!this.errorMessage());

  protected readonly selectedLabel = computed(() => {
    const val = this.selectedValue();
    if (val === undefined || val === null || val === '') return '';

    const opts = this.options();
    const optValueKey = this.optionValue();

    for (const opt of opts) {
      const currentVal = optValueKey ? opt?.[optValueKey] : opt;
      if (this.areEqual(currentVal, val)) {
        return this.getOptionLabel(opt);
      }
    }
    return '';
  });

  protected readonly computedTriggerClasses = computed(() => {
    const classes = [
      'w-full flex items-center justify-between text-left text-sm font-normal select-none',
      'h-(--ds-control-height-md) px-3.5',
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

  private areEqual(a: any, b: any): boolean {
    if (a === b) return true;
    if (a && b && typeof a === 'object' && typeof b === 'object') {
      if (a.id && b.id) return a.id === b.id;
    }
    return false;
  }

  protected onRadixValueChange(val: any): void {
    this.selectedValue.set(val);
    this.onChangeCallback(val);
    this.onTouchedCallback();

    const eventPayload = { value: val, originalEvent: null };
    this.change.emit(eventPayload);
    this.onChange.emit(eventPayload);
  }

  // ControlValueAccessor
  private onChangeCallback: (val: any) => void = () => {};
  private onTouchedCallback: () => void = () => {};

  writeValue(value: any): void {
    this.selectedValue.set(value);
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
