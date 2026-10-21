import {
  Component,
  input,
  output,
  signal,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';

export interface DsFileSelectEvent {
  files: File[];
  originalEvent: Event;
}

/**
 * Enterprise Design System File Upload component.
 * Replaces PrimeNG FileUpload with an accessible drag-and-drop dropzone & browse button.
 * Validates accept types, file sizes, and emits compatible onSelect events.
 */
@Component({
  selector: 'ds-file-upload',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="w-full flex flex-col gap-2 select-none"
      [class.opacity-50]="disabled()"
    >
      <!-- Dropzone Container -->
      <div
        (dragover)="handleDragOver($event)"
        (dragleave)="handleDragLeave($event)"
        (drop)="handleDrop($event)"
        (click)="triggerFileInput(fileInput)"
        [class]="computedDropzoneClasses()"
        role="button"
        [attr.tabindex]="disabled() ? -1 : 0"
        (keydown.enter)="triggerFileInput(fileInput)"
        (keydown.space)="triggerFileInput(fileInput)"
      >
        <input
          #fileInput
          type="file"
          class="hidden"
          [accept]="accept()"
          [multiple]="multiple()"
          [disabled]="disabled()"
          (change)="handleFileInputChange($event)"
        />

        <div class="flex flex-col items-center justify-center text-center p-6 gap-2">
          <div
            class="w-12 h-12 rounded-full bg-primary-50 dark:bg-primary-950/60 text-(--ds-color-primary) flex items-center justify-center text-xl shrink-0"
            aria-hidden="true"
          >
            <i [class]="chooseIcon()"></i>
          </div>

          <div>
            <span class="text-sm font-semibold text-slate-800 dark:text-slate-200">
              {{ chooseLabel() }}
            </span>
            <span class="text-xs text-slate-500 dark:text-slate-400 block mt-0.5">
              or drag & drop here
            </span>
          </div>

          @if (accept()) {
            <span class="text-[11px] text-slate-400 dark:text-slate-500">
              Supported formats: {{ accept() }}
            </span>
          }
        </div>
      </div>

      <!-- Selected File Badges -->
      @if (selectedFiles().length > 0) {
        <div class="flex flex-col gap-1.5 mt-1">
          @for (file of selectedFiles(); track file.name; let idx = $index) {
            <div
              class="flex items-center justify-between p-2.5 rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900/60 text-xs"
            >
              <div class="flex items-center gap-2 truncate">
                <i class="pi pi-file text-slate-400 text-sm shrink-0" aria-hidden="true"></i>
                <span class="font-medium text-slate-800 dark:text-slate-200 truncate">
                  {{ file.name }}
                </span>
                <span class="text-slate-400 dark:text-slate-500 shrink-0">
                  ({{ formatBytes(file.size) }})
                </span>
              </div>

              <button
                type="button"
                (click)="removeFile(idx, $event)"
                class="p-1 rounded text-slate-400 hover:text-red-500 dark:hover:text-red-400 transition-colors focus:outline-none"
                aria-label="Remove file"
              >
                <i class="pi pi-times text-xs"></i>
              </button>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class DsFileUploadComponent {
  readonly chooseLabel = input<string>('Browse files');
  readonly chooseIcon = input<string>('pi pi-upload');
  readonly accept = input<string>('');
  readonly maxFileSize = input<number>(52428800); // 50MB
  readonly multiple = input<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly styleClass = input<string>('');

  readonly select = output<DsFileSelectEvent>();
  readonly onSelect = output<DsFileSelectEvent>(); // PrimeNG compatibility alias

  protected readonly isDragOver = signal<boolean>(false);
  protected readonly selectedFiles = signal<File[]>([]);

  protected triggerFileInput(inputEl: HTMLInputElement): void {
    if (!this.disabled()) {
      inputEl.click();
    }
  }

  protected readonly computedDropzoneClasses = computed(() => {
    const classes = [
      'w-full border-2 border-dashed rounded-(--ds-control-radius) transition-all duration-150',
      'focus:outline-none focus:ring-2 focus:ring-(--ds-focus-ring-color)',
    ];

    if (this.disabled()) {
      classes.push('cursor-not-allowed opacity-60 bg-slate-100 dark:bg-slate-900/20 border-slate-200 dark:border-slate-800');
    } else {
      classes.push('cursor-pointer bg-white dark:bg-slate-900/40');
      if (this.isDragOver()) {
        classes.push(
          'border-(--ds-color-primary) bg-primary-50/40 dark:bg-primary-950/30'
        );
      } else {
        classes.push(
          'border-(--ds-input-border) hover:border-(--ds-color-primary) hover:bg-slate-50/50 dark:hover:bg-slate-800/30'
        );
      }
    }

    if (this.styleClass()) {
      classes.push(this.styleClass());
    }

    return classes.join(' ');
  });

  protected handleDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (!this.disabled()) {
      this.isDragOver.set(true);
    }
  }

  protected handleDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  protected handleDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
    if (this.disabled()) return;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      const filesArray = Array.from(event.dataTransfer.files);
      this.processFiles(filesArray, event);
    }
  }

  protected handleFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const filesArray = Array.from(input.files);
      this.processFiles(filesArray, event);
    }
  }

  private processFiles(files: File[], originalEvent: Event): void {
    const valid = files.filter((f) => f.size <= this.maxFileSize());
    this.selectedFiles.set(valid);

    const eventPayload: DsFileSelectEvent = {
      files: valid,
      originalEvent,
    };
    this.select.emit(eventPayload);
    this.onSelect.emit(eventPayload);
  }

  protected removeFile(index: number, event: MouseEvent): void {
    event.stopPropagation();
    const updated = [...this.selectedFiles()];
    updated.splice(index, 1);
    this.selectedFiles.set(updated);

    const eventPayload: DsFileSelectEvent = {
      files: updated,
      originalEvent: event,
    };
    this.select.emit(eventPayload);
    this.onSelect.emit(eventPayload);
  }

  protected formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
  }
}
