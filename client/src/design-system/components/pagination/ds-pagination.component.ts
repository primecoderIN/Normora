import {
  Component,
  input,
  output,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RdxPaginationModule } from '@radix-ng/primitives/pagination';

export interface DsPageState {
  first: number;
  rows: number;
  page: number;
  pageCount: number;
}

/**
 * Enterprise Design System Pagination component.
 * Powered directly by @radix-ng/primitives/pagination headless primitive.
 * Combines Radix NG accessible navigation items and native transformedRange with
 * customizable page size selector and summary counts.
 */
@Component({
  selector: 'ds-pagination',
  standalone: true,
  imports: [CommonModule, RdxPaginationModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-wrap items-center justify-between gap-4 py-3 px-4 border-t border-slate-200 dark:border-slate-800 text-xs text-slate-600 dark:text-slate-400 select-none"
    >
      <!-- Summary -->
      <div class="flex items-center gap-2">
        <span>
          Showing <span class="font-semibold text-slate-900 dark:text-slate-100">{{ startRecord() }}</span> to
          <span class="font-semibold text-slate-900 dark:text-slate-100">{{ endRecord() }}</span> of
          <span class="font-semibold text-slate-900 dark:text-slate-100">{{ totalRecords() }}</span> entries
        </span>

        @if (rowsPerPageOptions().length > 0) {
          <div class="flex items-center gap-1.5 ml-3">
            <span>Per page:</span>
            <select
              [value]="rows()"
              (change)="handleRowsChange($event)"
              class="h-7 px-2 text-xs rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-(--ds-color-primary) cursor-pointer"
            >
              @for (option of rowsPerPageOptions(); track option) {
                <option [value]="option">{{ option }}</option>
              }
            </select>
          </div>
        }
      </div>

      <!-- Radix NG Pagination Root -->
      <nav
        rdxPaginationRoot
        [total]="totalRecords()"
        [itemsPerPage]="rows()"
        [page]="currentRadixPage()"
        (pageChange)="onRadixPageChange($event)"
        class="flex items-center gap-1"
      >
        <!-- First Page Button -->
        <button
          rdxPaginationFirst
          type="button"
          class="w-7 h-7 flex items-center justify-center rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          aria-label="First page"
        >
          <i class="pi pi-angle-double-left text-xs" aria-hidden="true"></i>
        </button>

        <!-- Previous Page Button -->
        <button
          rdxPaginationPrev
          type="button"
          class="w-7 h-7 flex items-center justify-center rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          aria-label="Previous page"
        >
          <i class="pi pi-angle-left text-xs" aria-hidden="true"></i>
        </button>

        <!-- Page List via Radix NG transformedRange -->
        <div rdxPaginationList #paginationList="rdxPaginationList" class="flex items-center gap-1">
          @for (item of paginationList.transformedRange(); track $index) {
            @if (item.type === 'page') {
              <button
                rdxPaginationListItem
                [value]="item.value"
                type="button"
                class="min-w-7 h-7 px-2 flex items-center justify-center rounded font-medium transition-colors cursor-pointer border border-(--border) bg-white dark:bg-slate-900 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 data-selected:bg-(--primary) data-selected:border-(--primary) data-selected:text-(--primary-foreground) data-selected:shadow-xs"
                [class.bg-(--primary)!]="item.value === currentRadixPage()"
                [class.text-white!]="item.value === currentRadixPage()"
              >
                {{ item.value }}
              </button>
            } @else {
              <span
                rdxPaginationEllipsis
                class="min-w-7 h-7 flex items-center justify-center text-slate-400 select-none"
              >
                &#8230;
              </span>
            }
          }
        </div>

        <!-- Next Page Button -->
        <button
          rdxPaginationNext
          type="button"
          class="w-7 h-7 flex items-center justify-center rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          aria-label="Next page"
        >
          <i class="pi pi-angle-right text-xs" aria-hidden="true"></i>
        </button>

        <!-- Last Page Button -->
        <button
          rdxPaginationLast
          type="button"
          class="w-7 h-7 flex items-center justify-center rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          aria-label="Last page"
        >
          <i class="pi pi-angle-double-right text-xs" aria-hidden="true"></i>
        </button>
      </nav>
    </div>
  `,
})
export class DsPaginationComponent {
  readonly first = input<number>(0);
  readonly rows = input<number>(10);
  readonly totalRecords = input<number>(0);
  readonly rowsPerPageOptions = input<number[]>([10, 20, 50]);

  readonly onPageChange = output<DsPageState>();
  readonly pageChange = output<DsPageState>();

  protected readonly totalPages = computed(() => {
    const r = this.rows() || 1;
    return Math.max(1, Math.ceil(this.totalRecords() / r));
  });

  protected readonly zeroBasedPage = computed(() => {
    const r = this.rows() || 1;
    return Math.floor(this.first() / r);
  });

  protected readonly currentRadixPage = computed(() => {
    return this.zeroBasedPage() + 1;
  });

  protected readonly startRecord = computed(() => {
    if (this.totalRecords() === 0) return 0;
    return this.first() + 1;
  });

  protected readonly endRecord = computed(() => {
    return Math.min(this.first() + this.rows(), this.totalRecords());
  });

  protected onRadixPageChange(pageOneBased: number): void {
    const pageIndex = Math.max(0, pageOneBased - 1);
    const newFirst = pageIndex * this.rows();
    const state: DsPageState = {
      first: newFirst,
      rows: this.rows(),
      page: pageIndex,
      pageCount: this.totalPages(),
    };
    this.onPageChange.emit(state);
    this.pageChange.emit(state);
  }

  protected handleRowsChange(event: Event): void {
    const newRows = Number((event.target as HTMLSelectElement).value);
    const state: DsPageState = {
      first: 0,
      rows: newRows,
      page: 0,
      pageCount: Math.ceil(this.totalRecords() / newRows),
    };
    this.onPageChange.emit(state);
    this.pageChange.emit(state);
  }
}
