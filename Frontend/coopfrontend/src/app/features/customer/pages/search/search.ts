import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';

import { Category } from '../../../../core/models/category.models';
import { OfferSummary } from '../../../../core/models/marketplace.models';
import { CategoryService } from '../../../../core/services/category.service';
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
export class SearchComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly marketplace = inject(MarketplaceService);
  private readonly categoryService = inject(CategoryService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly offers = signal<OfferSummary[]>([]);
  readonly totalCount = signal(0);
  readonly pageNumber = signal(1);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)));
  readonly searchTerm = signal('');
  readonly categoryFilter = signal<string | null>(null);

  readonly categoryName = computed(() => {
    const id = this.categoryFilter();
    return id ? this.categoryService.nameAr(id) || null : null;
  });

  constructor() {
    this.categoryService
      .getAll()
      .pipe(catchError(() => of<Category[]>([])))
      .subscribe();

    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.searchTerm.set(params.get('q') ?? '');
      this.categoryFilter.set(params.get('categoryId'));
      this.pageNumber.set(1);
      this.load();
    });
  }

  clearCategoryFilter(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { categoryId: null },
      queryParamsHandling: 'merge'
    });
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.marketplace
      .searchOffers({
        search: this.searchTerm() || null,
        categoryId: this.categoryFilter(),
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
