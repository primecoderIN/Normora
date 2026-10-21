import {
  Component,
  input,
  output,
  signal,
  forwardRef,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import {
  RdxCheckboxRootDirective,
  RdxCheckboxButtonDirective,
  RdxCheckboxIndicatorDirective,
  RdxCheckboxInputDirective,
} from '@radix-ng/primitives/checkbox';
import { RdxLabelDirective } from '@radix-ng/primitives/label';

/**
 * Enterprise Design System Checkbox component.
 * Powered directly by @radix-ng/primitives/checkbox and @radix-ng/primitives/label headless primitives.
 * Fully accessible, keyboard-trappable, and integrates with Angular Forms.
 */
@Component({
  selector: 'ds-checkbox',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RdxCheckboxRootDirective,
    RdxCheckboxButtonDirective,
    RdxCheckboxIndicatorDirective,
    RdxCheckboxInputDirective,
    RdxLabelDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DsCheckboxComponent),
      multi: true,
    },
  ],
  template: `
    <label
      rdxLabel
      class="inline-flex items-center gap-2.5 cursor-pointer select-none"
      [class.opacity-50]="disabled()"
      [class.cursor-not-allowed]="disabled()"
    >
      <span
        rdxCheckboxRoot
        [checked]="checked()"
        [disabled]="disabled()"
        (checkedChange)="onRadixCheckedChange($event)"
        class="inline-flex items-center justify-center relative"
      >
        <button
          rdxCheckboxButton
          type="button"
          class="w-4 h-4 rounded border flex items-center justify-center transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-(--ring) border-(--input) bg-white dark:bg-slate-900 hover:border-(--ds-input-border-hover) data-[state=checked]:bg-(--primary) data-[state=checked]:border-(--primary) data-[state=checked]:text-white data-[state=checked]:shadow-xs data-disabled:opacity-50 data-disabled:cursor-not-allowed"
        >
          <span rdxCheckboxIndicator class="inline-flex items-center justify-center text-white">
            <svg
              class="w-3 h-3"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="3"
              stroke-linecap="round"
              stroke-linejoin="round"
            >
              <polyline points="20 6 9 17 4 12"></polyline>
            </svg>
          </span>
        </button>
        <input rdxCheckboxInput class="sr-only" type="checkbox" />
      </span>

      @if (label()) {
        <span class="text-sm font-medium text-slate-700 dark:text-slate-200">
          {{ label() }}
        </span>
      }
    </label>
  `,
})
export class DsCheckboxComponent implements ControlValueAccessor {
  readonly label = input<string | undefined>(undefined);
  readonly disabled = input<boolean>(false);

  readonly checkedChange = output<boolean>();
  readonly onChange = output<boolean>(); // PrimeNG compatibility alias

  protected readonly checked = signal<boolean>(false);
  private readonly formDisabled = signal<boolean>(false);

  protected onRadixCheckedChange(val: boolean): void {
    this.checked.set(val);
    this.onChangeCallback(val);
    this.onTouchedCallback();
    this.checkedChange.emit(val);
    this.onChange.emit(val);
  }

  private onChangeCallback: (val: boolean) => void = () => {};
  private onTouchedCallback: () => void = () => {};

  writeValue(val: boolean): void {
    this.checked.set(!!val);
  }

  registerOnChange(fn: (val: boolean) => void): void {
    this.onChangeCallback = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouchedCallback = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.formDisabled.set(isDisabled);
  }
}
