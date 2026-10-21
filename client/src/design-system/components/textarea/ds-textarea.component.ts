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

/**
 * Enterprise Design System Textarea.
 * Built directly on top of @radix-ng/primitives/field headless primitives.
 * Implements ControlValueAccessor and uses the centralized input-border tokens.
 */
@Component({
  selector: 'ds-textarea',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RdxFieldRoot,
    RdxFieldLabel,
    RdxFieldControl,
    RdxFieldDescription,
    RdxFieldError,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DsTextareaComponent),
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

      <textarea
        rdxFieldControl
        [id]="inputId()"
        [name]="name() || inputId()"
        [rows]="rows()"
        [placeholder]="placeholder()"
        [disabled]="isDisabled()"
        [readOnly]="readonly()"
        [required]="required()"
        [value]="value()"
        (input)="handleInput($event)"
        (blur)="handleBlur()"
        (focus)="handleFocus()"
        [class]="computedTextareaClasses()"
      ></textarea>

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
export class DsTextareaComponent implements ControlValueAccessor {
  private static nextId = 0;

  readonly label = input<string | undefined>(undefined);
  readonly placeholder = input<string>('');
  readonly rows = input<number>(3);
  readonly id = input<string | undefined>(undefined);
  readonly name = input<string | undefined>(undefined);
  readonly disabled = input<boolean>(false);
  readonly readonly = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly errorMessage = input<string | undefined>(undefined);
  readonly hint = input<string | undefined>(undefined);
  readonly containerClass = input<string>('');
  readonly textareaClass = input<string>('');

  readonly valueChange = output<string>();
  readonly textareaBlur = output<void>();
  readonly textareaFocus = output<void>();

  protected readonly value = signal<string>('');
  private readonly formDisabled = signal<boolean>(false);

  private readonly autoId = `ds-textarea-${++DsTextareaComponent.nextId}`;

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

  protected readonly computedTextareaClasses = computed(() => {
    const classes = [
      'w-full font-normal transition-all duration-150 ease-in-out py-2.5 px-3.5 text-sm',
      'rounded-(--radius) border resize-y custom-scrollbar',
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

    if (this.textareaClass()) {
      classes.push(this.textareaClass());
    }

    return classes.join(' ');
  });

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
    const val = (event.target as HTMLTextAreaElement).value;
    this.value.set(val);
    this.onChange(val);
    this.valueChange.emit(val);
  }

  protected handleBlur(): void {
    this.onTouched();
    this.textareaBlur.emit();
  }

  protected handleFocus(): void {
    this.textareaFocus.emit();
  }
}
