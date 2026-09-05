import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AskCitation, AskService } from '../../../core/services/ask.service';

@Component({
  imports: [FormsModule],
  selector: 'app-ask',
  styleUrl: './ask.css',
  templateUrl: './ask.html',
})
export class Ask {
  private askService = inject(AskService);

  question = '';
  answer = signal('');
  sources = signal<AskCitation[]>([]);
  isLoading = signal(false);
  error = signal('');
  hasAsked = signal(false);

  ask() {
    const question = this.question.trim();
    if (!question || this.isLoading()) return;

    this.isLoading.set(true);
    this.error.set('');
    this.answer.set('');
    this.sources.set([]);
    this.hasAsked.set(true);

    this.askService.ask(question).subscribe({
      next: result => {
        this.answer.set(result.answer);
        this.sources.set(result.sources ?? []);
        this.isLoading.set(false);
      },
      error: error => {
        this.error.set(error.error?.message || 'I could not answer that right now.');
        this.isLoading.set(false);
      }
    });
  }

  useSuggestion(question: string) {
    this.question = question;
    this.ask();
  }

  formatSimilarity(similarity: number): string {
    return `${Math.round(similarity * 100)}%`;
  }
}
