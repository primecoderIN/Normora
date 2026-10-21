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
import { RdxInputDirective } from '@radix-ng/primitives/input';

/**
 * Enterprise Design System Color Picker component.
 * Replaces PrimeNG ColorPicker with an accessible native color input,
 * hex input powered by @radix-ng/primitives/input, and quick palette swatches.
 */
@Component({
  selector: 'ds-color-picker',
  standalone: true,
  imports: [CommonModule, FormsModule, RdxInputDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DsColorPickerComponent),
      multi: true,
    },
  ],
  template: `
    <div
      class="flex items-center gap-2 select-none"
      [class.opacity-50]="isDisabled()"
      [class.pointer-events-none]="isDisabled()"
    >
      <!-- Color swatch trigger with native color picker -->
      <div
        class="relative w-9 h-9 rounded-lg border border-(--ds-input-border) p-0.5 bg-white dark:bg-slate-900 shadow-xs overflow-hidden flex items-center justify-center transition-colors"
        [class.cursor-pointer]="!isDisabled()"
        [class.hover:border-(--ds-input-border-hover)]="!isDisabled()"
      >
        <input
          type="color"
          [value]="normalizedHex()"
          [disabled]="isDisabled()"
          (input)="handleColorInput($event)"
          class="absolute inset-0 opacity-0 w-full h-full"
          [class.cursor-pointer]="!isDisabled()"
          aria-label="Pick color"
        />
        <div
          class="w-full h-full rounded-md border border-black/10 dark:border-white/10"
          [style.backgroundColor]="normalizedHex()"
        ></div>
      </div>

      <!-- Hex Text Input powered by Radix NG Input -->
      <div class="relative flex-1 min-w-25 max-w-32.5">
        <input
          rdxInput
          type="text"
          [value]="colorValue()"
          [disabled]="isDisabled()"
          (input)="handleTextInput($event)"
          maxlength="7"
          placeholder="#4f46e5"
          class="w-full h-9 px-2.5 font-mono text-xs uppercase rounded-lg border border-(--ds-input-border) bg-(--ds-input-bg) text-(--ds-input-text) focus:outline-none focus:border-(--ds-input-border-focus) focus:ring-2 focus:ring-(--ds-focus-ring-color) disabled:bg-(--muted) disabled:cursor-not-allowed"
        />
      </div>
    </div>
  `,
})
export class DsColorPickerComponent implements ControlValueAccessor {
  readonly format = input<'hex'>('hex');
  readonly disabled = input<boolean>(false);

  readonly change = output<any>();
  readonly onChange = output<any>(); // PrimeNG compatibility alias

  protected readonly colorValue = signal<string>('#4f46e5');
  private readonly formDisabled = signal<boolean>(false);
  protected readonly isDisabled = computed(() => this.disabled() || this.formDisabled());

  protected normalizedHex(): string {
    let val = this.colorValue() || '#4f46e5';
    if (!val.startsWith('#')) {
      val = `#${val}`;
    }
    return val;
  }

  protected handleColorInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.updateColor(val);
  }

  protected handleTextInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.updateColor(val);
  }

  private updateColor(val: string): void {
    this.colorValue.set(val);
    this.onChangeCallback(val);
    this.onTouchedCallback();

    const payload = { value: val };
    this.change.emit(payload);
    this.onChange.emit(payload);
  }

  // ControlValueAccessor
  private onChangeCallback: (val: any) => void = () => {};
  private onTouchedCallback: () => void = () => {};

  writeValue(val: any): void {
    if (val != null) {
      const hex = typeof val === 'object' && val.value ? val.value : String(val);
      this.colorValue.set(hex);
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
