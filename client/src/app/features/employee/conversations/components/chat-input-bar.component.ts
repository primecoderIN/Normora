import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

/**
 * Component responsible solely for the chat input bar, textarea auto-resizing,
 * keyboard shortcuts, and message submission dispatching.
 */
@Component({
  selector: 'app-chat-input-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-4 md:p-6 bg-white dark:bg-slate-900 border-t border-slate-200 dark:border-slate-700 flex-none">
      <form
        class="flex items-end gap-3 p-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-700 rounded-2xl transition-all focus-within:border-primary-300 focus-within:ring-4 focus-within:ring-primary-100/50 shadow-[0_2px_12px_rgba(0,0,0,0.02)]"
        (ngSubmit)="onSubmit()"
      >
        <button
          type="button"
          class="flex-none flex items-center justify-center w-10 h-10 rounded-xl text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800 hover:text-slate-600 dark:hover:text-slate-300 transition-colors cursor-pointer"
          title="Attach file"
          (click)="onAttachFile.emit()"
          aria-label="Attach file"
        >
          <i class="pi pi-paperclip text-xl"></i>
        </button>

        <textarea
          #textareaEl
          class="flex-1 bg-transparent border-none! outline-none! focus:outline-none! focus:border-transparent! focus:ring-0! resize-none px-2 py-2.5 text-[0.95rem] text-slate-900 dark:text-white placeholder:text-slate-400 font-sans leading-relaxed max-h-32 custom-scrollbar disabled:opacity-50"
          [(ngModel)]="question"
          name="question"
          rows="1"
          maxlength="1000"
          placeholder="Ask a question..."
          [disabled]="isSending"
          (keydown)="onKeydown($event)"
          (input)="autoResize()"
          aria-label="Your message"
        ></textarea>

        <button
          type="submit"
          class="flex items-center justify-center flex-none w-10 h-10 bg-primary-600 text-white border-none rounded-xl cursor-pointer transition-all hover:bg-primary-700 hover:shadow-md disabled:bg-slate-200 dark:disabled:bg-slate-800 disabled:text-slate-400 disabled:cursor-not-allowed disabled:shadow-none"
          [disabled]="!question.trim() || isSending"
          aria-label="Send message"
        >
          <i
            class="pi text-xl"
            [class.pi-spin]="isSending"
            [class.pi-spinner]="isSending"
            [class.pi-send]="!isSending"
            [class.-ml-1]="!isSending"
            [class.mt-1]="!isSending"
          ></i>
        </button>
      </form>

      <p class="text-center text-[0.7rem] text-slate-400 mt-2 mb-0">
        Press <kbd class="px-1 py-0.5 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded text-[0.65rem] font-sans">Enter</kbd> to send ·
        <kbd class="px-1 py-0.5 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded text-[0.65rem] font-sans">Shift+Enter</kbd> for new line
      </p>
    </div>
  `
})
export class ChatInputBarComponent {
  @Input() isSending = false;

  @Output() onSendMessage = new EventEmitter<string>();
  @Output() onAttachFile = new EventEmitter<void>();

  @ViewChild('textareaEl') private textareaEl!: ElementRef<HTMLTextAreaElement>;

  question = '';

  setValue(text: string): void {
    this.question = text;
    this.focus();
    setTimeout(() => this.autoResize());
  }

  focus(): void {
    if (this.textareaEl?.nativeElement) {
      this.textareaEl.nativeElement.focus();
    }
  }

  onSubmit(): void {
    const text = this.question.trim();
    if (text && !this.isSending) {
      this.onSendMessage.emit(text);
      this.question = '';
      this.resetTextareaHeight();
    }
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSubmit();
    }
  }

  autoResize(): void {
    const el = this.textareaEl?.nativeElement;
    if (el) {
      el.style.height = 'auto';
      el.style.height = Math.min(el.scrollHeight, 128) + 'px';
    }
  }

  private resetTextareaHeight(): void {
    setTimeout(() => {
      const el = this.textareaEl?.nativeElement;
      if (el) el.style.height = 'auto';
    });
  }
}
