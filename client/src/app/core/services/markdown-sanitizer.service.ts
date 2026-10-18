import { Injectable } from '@angular/core';
import { marked } from 'marked';
import DOMPurify from 'dompurify';

/**
 * Service responsible solely for parsing Markdown text and sanitizing it into safe HTML.
 * Adheres to SRP by abstracting marked + DOMPurify integration and caching away from UI components.
 */
@Injectable({
  providedIn: 'root'
})
export class MarkdownSanitizerService {
  private readonly cache = new Map<string, string>();
  private readonly maxCacheSize = 150;

  /**
   * Parses markdown and sanitizes HTML. Uses an in-memory bounded cache for completed text.
   *
   * @param content The raw markdown string
   * @param cacheable Whether to cache the result (true by default, set false for rapid streaming chunks)
   */
  render(content: string, cacheable = true): string {
    if (!content) return '';

    if (cacheable) {
      const cached = this.cache.get(content);
      if (cached) return cached;
    }

    const parsed = marked.parse(content, { async: false, breaks: true }) as string;
    const sanitized = DOMPurify.sanitize(parsed);

    if (cacheable && content.length > 50) {
      this.cache.set(content, sanitized);
      if (this.cache.size > this.maxCacheSize) {
        const firstKey = this.cache.keys().next().value;
        if (firstKey) {
          this.cache.delete(firstKey);
        }
      }
    }

    return sanitized;
  }

  /** Clears the markdown parsing cache if necessary. */
  clearCache(): void {
    this.cache.clear();
  }
}
