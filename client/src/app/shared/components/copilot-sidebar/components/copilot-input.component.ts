import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-copilot-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-3 border-t border-slate-100 dark:border-slate-800 bg-white dark:bg-slate-900 shrink-0">
      <form (ngSubmit)="handleSend()" class="flex flex-col gap-2">
        <div class="relative flex items-center rounded-xl bg-slate-50 dark:bg-slate-800/80 border border-slate-200 dark:border-slate-700 focus-within:border-primary-500 focus-within:ring-2 focus-within:ring-primary-500/20 transition-all">
          <textarea
            [ngModel]="userInput()"
            (ngModelChange)="userInput.set($event)"
            name="copilotInput"
            rows="2"
            (keydown.enter)="handleKeyDown($event)"
            placeholder="Ask anything about company policies..."
            [disabled]="isSending()"
            class="w-full px-3.5 py-2.5 text-xs bg-transparent text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 resize-none border-none! outline-none! shadow-none!"
          ></textarea>
          
          <button
            type="submit"
            [disabled]="!userInput().trim() || isSending()"
            aria-label="Send query"
            class="absolute right-2.5 bottom-2.5 flex items-center justify-center w-7 h-7 rounded-lg bg-primary-600 hover:bg-primary-500 text-white disabled:opacity-40 disabled:cursor-not-allowed transition-all cursor-pointer shadow-sm"
          >
            @if (isSending()) {
              <i class="pi pi-spin pi-spinner text-xs"></i>
            } @else {
              <i class="pi pi-arrow-up text-xs"></i>
            }
          </button>
        </div>

        <div class="flex items-center justify-between px-1 text-[10px] text-slate-400">
          <span>Enter to send • Shift+Enter for newline</span>
          <span>Esc to close</span>
        </div>
      </form>
    </div>
  `
})
export class CopilotInputComponent {
  isSending = input<boolean>(false);
  onSubmitPrompt = output<string>();

  userInput = signal('');

  handleKeyDown(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    if (keyboardEvent.key === 'Enter' && !keyboardEvent.shiftKey) {
      keyboardEvent.preventDefault();
      this.handleSend();
    }
  }

  handleSend(): void {
    const text = this.userInput().trim();
    if (!text || this.isSending()) return;
    this.onSubmitPrompt.emit(text);
    this.userInput.set('');
  }
}
