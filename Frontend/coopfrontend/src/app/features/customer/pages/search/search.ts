import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { OfferSummary } from '../../../../core/models/marketplace.models';
import { MarketplaceService } from '../../../../core/services/marketplace.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { OfferCardComponent } from '../../components/offer-card/offer-card';

type PageStatus = 'loading' | 'error' | 'loaded';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-customer-search',
  imports: [
    UiCardComponent,
    UiButtonComponent,
    UiAlertComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    OfferCardComponent
  ],
  templateUrl: './search.html',
  styleUrl: './search.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly marketplace = inject(MarketplaceService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly offers = signal<OfferSummary[]>([]);
  readonly totalCount = signal(0);
  readonly pageNumber = signal(1);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)));
  readonly searchTerm = signal('');

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.searchTerm.set(params.get('q') ?? '');
      this.pageNumber.set(1);
      this.load();
    });
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.marketplace
      .searchOffers({
        search: this.searchTerm() || null,
        pageNumber: this.pageNumber(),
        pageSize: PAGE_SIZE
      })
      .subscribe({
        next: (result) => {
          this.offers.set(result.items);
          this.totalCount.set(result.totalCount);
          this.status.set('loaded');
        },
        error: (err: unknown) => {
          this.loadErrorMessage.set(extractErrorMessage(err));
          this.status.set('error');
        }
      });
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
