import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

import { MerchantSummary } from '../../../../core/models/marketplace.models';
import { MarketplaceService } from '../../../../core/services/marketplace.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { MerchantCardComponent } from '../../components/merchant-card/merchant-card';

type PageStatus = 'loading' | 'error' | 'loaded';

const PAGE_SIZE = 12;
const SEARCH_DEBOUNCE_MS = 400;

@Component({
  selector: 'app-customer-merchants',
  imports: [UiAlertComponent, UiButtonComponent, UiEmptyStateComponent, UiSpinnerComponent, MerchantCardComponent],
  templateUrl: './merchants.html',
  styleUrl: './merchants.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MerchantsComponent {
  private readonly marketplace = inject(MarketplaceService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly merchants = signal<MerchantSummary[]>([]);
  readonly totalCount = signal(0);
  readonly pageNumber = signal(1);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)));
  readonly searchInputValue = signal('');

  private readonly searchInput$ = new Subject<string>();

  constructor() {
    this.load();

    this.searchInput$
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.pageNumber.set(1);
        this.load();
      });
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.marketplace
      .searchMerchants({
        search: this.searchInputValue().trim() || null,
        pageNumber: this.pageNumber(),
        pageSize: PAGE_SIZE
      })
      .subscribe({
        next: (result) => {
          this.merchants.set(result.items);
          this.totalCount.set(result.totalCount);
          this.status.set('loaded');
        },
        error: (err: unknown) => {
          this.loadErrorMessage.set(extractErrorMessage(err));
          this.status.set('error');
        }
      });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInputValue.set(value);
    this.searchInput$.next(value.trim());
  }

  goToPreviousPage(): void {
    if (this.pageNumber() <= 1) {
      return;
    }

    this.pageNumber.update((value) => value - 1);
    this.load();
  }

  goToNextPage(): void {
    if (this.pageNumber() >= this.totalPages()) {
      return;
    }

    this.pageNumber.update((value) => value + 1);
    this.load();
  }
}
