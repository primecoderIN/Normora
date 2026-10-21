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
    <div class="flex items-center gap-2 select-none">
      <!-- Color swatch trigger with native color picker -->
      <div class="relative w-9 h-9 rounded-lg border border-(--ds-input-border) p-0.5 bg-white dark:bg-slate-900 shadow-xs cursor-pointer overflow-hidden flex items-center justify-center hover:border-(--ds-input-border-hover) transition-colors">
        <input
          type="color"
          [value]="normalizedHex()"
          [disabled]="disabled()"
          (input)="handleColorInput($event)"
          class="absolute inset-0 opacity-0 cursor-pointer w-full h-full"
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
          [disabled]="disabled()"
          (input)="handleTextInput($event)"
          maxlength="7"
          placeholder="#4f46e5"
          class="w-full h-9 px-2.5 font-mono text-xs uppercase rounded-lg border border-(--ds-input-border) bg-(--ds-input-bg) text-(--ds-input-text) focus:outline-none focus:border-(--ds-input-border-focus) focus:ring-2 focus:ring-(--ds-focus-ring-color)"
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
    if (val) {
      this.colorValue.set(String(val));
    }
  }

  registerOnChange(fn: (val: any) => void): void {
    this.onChangeCallback = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouchedCallback = fn;
  }

  setDisabledState?(isDisabled: boolean): void {}
}
