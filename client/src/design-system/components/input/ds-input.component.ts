import {
  Component,
  input,
  output,
  signal,
  forwardRef,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';

import {
  RdxFieldRoot,
  RdxFieldLabel,
  RdxFieldControl,
  RdxFieldDescription,
  RdxFieldError,
} from '@radix-ng/primitives/field';
import { RdxInputDirective } from '@radix-ng/primitives/input';

export type DsInputSize = 'sm' | 'md' | 'lg';

/**
 * Enterprise Design System Text Input.
 * Built directly on top of @radix-ng/primitives/field and @radix-ng/primitives/input headless primitives.
 * Implements ControlValueAccessor for seamless Angular Reactive Forms integration.
 * Anchored to centralized design tokens for borders, heights, focus rings, and error states.
 */
@Component({
  selector: 'ds-input',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RdxFieldRoot,
    RdxFieldLabel,
    RdxFieldControl,
    RdxFieldDescription,
    RdxFieldError,
    RdxInputDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DsInputComponent),
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

      <div class="relative flex items-center w-full">
        @if (prefixIcon()) {
          <div
            class="absolute left-3 flex items-center pointer-events-none text-slate-400 dark:text-slate-500"
            aria-hidden="true"
          >
            <i [class]="prefixIcon()" class="text-sm"></i>
          </div>
        }

        <input
          rdxFieldControl
          rdxInput
          [id]="inputId()"
          [name]="name() || inputId()"
          [type]="type()"
          [placeholder]="placeholder()"
          [disabled]="isDisabled()"
          [readOnly]="readonly()"
          [required]="required()"
          [value]="value()"
          (input)="handleInput($event)"
          (blur)="handleBlur()"
          (focus)="handleFocus()"
          [class]="computedInputClasses()"
        />

        @if (clearable() && value() && !isDisabled() && !readonly()) {
          <button
            type="button"
            (click)="handleClear()"
            class="absolute right-3 p-1 rounded-full text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors focus:outline-none"
            aria-label="Clear input"
          >
            <i class="pi pi-times text-xs"></i>
          </button>
        } @else if (suffixIcon()) {
          <div
            class="absolute right-3 flex items-center pointer-events-none text-slate-400 dark:text-slate-500"
            aria-hidden="true"
          >
            <i [class]="suffixIcon()" class="text-sm"></i>
          </div>
        }
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
export class DsInputComponent implements ControlValueAccessor {
  private static nextId = 0;

  readonly label = input<string | undefined>(undefined);
  readonly placeholder = input<string>('');
  readonly type = input<'text' | 'email' | 'password' | 'number' | 'search' | 'url' | 'tel'>('text');
  readonly id = input<string | undefined>(undefined);
  readonly name = input<string | undefined>(undefined);
  readonly disabled = input<boolean>(false);
  readonly readonly = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly errorMessage = input<string | undefined>(undefined);
  readonly hint = input<string | undefined>(undefined);
  readonly prefixIcon = input<string | undefined>(undefined);
  readonly suffixIcon = input<string | undefined>(undefined);
  readonly clearable = input<boolean>(false);
  readonly size = input<DsInputSize>('md');
  readonly containerClass = input<string>('');
  readonly inputClass = input<string>('');

  readonly valueChange = output<string>();
  readonly inputBlur = output<void>();
  readonly inputFocus = output<void>();

  protected readonly value = signal<string>('');
  protected readonly isFocused = signal<boolean>(false);
  private readonly formDisabled = signal<boolean>(false);

  private readonly autoId = `ds-input-${++DsInputComponent.nextId}`;

  protected readonly inputId = computed(() => this.id() || this.autoId);
  protected readonly errorId = computed(() => `${this.inputId()}-error`);
  protected readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
  protected readonly isInvalid = computed(() => this.invalid() || !!this.errorMessage());

  protected readonly describedBy = computed(() => {
    if (this.isInvalid() && this.errorMessage()) {
      return this.errorId();
    }
    return null;
  });

  protected readonly computedInputClasses = computed(() => {
    const classes = [
      'w-full font-normal transition-all duration-150 ease-in-out',
      'rounded-(--radius) border',
      'bg-(--background) text-(--foreground)',
      'placeholder:text-(--ds-input-placeholder)',
      'border-(--input) hover:border-(--ds-input-border-hover)',
      'focus:outline-none focus:border-(--primary) focus:ring-2 focus:ring-(--ring)',
      'data-invalid:border-(--destructive) focus:data-invalid:border-(--destructive) focus:ring-2 focus:ring-red-500/20 dark:focus:ring-red-400/25',
      'disabled:bg-(--muted) disabled:text-(--muted-foreground) disabled:cursor-not-allowed',
      'data-disabled:bg-(--muted) data-disabled:text-(--muted-foreground) data-disabled:cursor-not-allowed',
    ];

    if (this.isInvalid()) {
      classes.push(
        'border-(--destructive) focus:border-(--destructive)',
        'focus:ring-2 focus:ring-red-500/20 dark:focus:ring-red-400/25'
      );
    }

    // Heights and Paddings
    switch (this.size()) {
      case 'sm':
        classes.push('h-(--ds-control-height-sm) text-xs');
        classes.push(this.prefixIcon() ? 'pl-8' : 'pl-2.5');
        classes.push(this.suffixIcon() || this.clearable() ? 'pr-8' : 'pr-2.5');
        break;
      case 'lg':
        classes.push('h-(--ds-control-height-lg) text-base');
        classes.push(this.prefixIcon() ? 'pl-11' : 'pl-4');
        classes.push(this.suffixIcon() || this.clearable() ? 'pr-11' : 'pr-4');
        break;
      case 'md':
      default:
        classes.push('h-(--ds-control-height-md) text-sm');
        classes.push(this.prefixIcon() ? 'pl-9' : 'pl-3.5');
        classes.push(this.suffixIcon() || this.clearable() ? 'pr-9' : 'pr-3.5');
        break;
    }

    if (this.inputClass()) {
      classes.push(this.inputClass());
    }

    return classes.join(' ');
  });

  // ControlValueAccessor callbacks
  private onChange: (val: string) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(val: any): void {
    this.value.set(val != null ? String(val) : '');
  }

  registerOnChange(fn: (val: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.formDisabled.set(isDisabled);
  }

  protected handleInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.value.set(val);
    this.onChange(val);
    this.valueChange.emit(val);
  }

  protected handleBlur(): void {
    this.isFocused.set(false);
    this.onTouched();
    this.inputBlur.emit();
  }

  protected handleFocus(): void {
    this.isFocused.set(true);
    this.inputFocus.emit();
  }

  protected handleClear(): void {
    this.value.set('');
    this.onChange('');
    this.valueChange.emit('');
  }
}
