import { NgTemplateOutlet } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, catchError, debounceTime, of } from 'rxjs';

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
  UiModalComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { OfferCardComponent } from '../../components/offer-card/offer-card';

type PageStatus = 'loading' | 'error' | 'loaded';

interface DiscountOption {
  value: number | null;
  label: string;
}

interface SortOption {
  value: string;
  label: string;
}

const PAGE_SIZE = 20;
const PRICE_CITY_DEBOUNCE_MS = 400;

const DISCOUNT_OPTIONS: DiscountOption[] = [
  { value: null, label: 'الكل' },
  { value: 10, label: '10% فأكثر' },
  { value: 25, label: '25% فأكثر' },
  { value: 50, label: '50% فأكثر' }
];

// Matches the exact values OffersController's SortBy switch understands —
// anything else (including omission) falls through to its highest-discount default.
const SORT_OPTIONS: SortOption[] = [
  { value: '', label: 'الأعلى خصماً' },
  { value: 'priceAsc', label: 'السعر: من الأقل للأعلى' },
  { value: 'priceDesc', label: 'السعر: من الأعلى للأقل' },
  { value: 'endingSoon', label: 'الأقرب انتهاءً' },
  { value: 'newest', label: 'الأحدث' }
];

function parseNumberParam(value: string | null): number | null {
  if (!value) {
    return null;
  }

  const num = Number(value);
  return Number.isFinite(num) ? num : null;
}

function parseNumberInput(value: string): number | null {
  const trimmed = value.trim();

  if (!trimmed) {
    return null;
  }

  const num = Number(trimmed);
  return Number.isFinite(num) ? num : null;
}

@Component({
  selector: 'app-customer-search',
  imports: [
    NgTemplateOutlet,
    UiCardComponent,
    UiButtonComponent,
    UiAlertComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiModalComponent,
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

  readonly discountOptions = DISCOUNT_OPTIONS;
  readonly sortOptions = SORT_OPTIONS;

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly offers = signal<OfferSummary[]>([]);
  readonly totalCount = signal(0);
  readonly pageNumber = signal(1);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)));

  readonly searchTerm = signal('');

  // Canonical filter state — always derived from the URL, never written to
  // directly from a template event. Every filter change goes through the URL so
  // a filtered search survives a refresh and can be shared as a link.
  readonly categoryFilter = signal<string | null>(null);
  readonly minPrice = signal<number | null>(null);
  readonly maxPrice = signal<number | null>(null);
  readonly minimumDiscount = signal<number | null>(null);
  readonly city = signal<string | null>(null);
  readonly sortBy = signal<string>('');

  // Draft state for the debounced text inputs — kept separate so typing doesn't
  // refetch on every keystroke, and re-seeded from the canonical values whenever
  // those change from outside (URL navigation, clearing a filter chip).
  readonly minPriceDraft = signal('');
  readonly maxPriceDraft = signal('');
  readonly cityDraft = signal('');

  readonly categoryTree = this.categoryService.categories;

  readonly categoryName = computed(() => {
    const id = this.categoryFilter();
    return id ? this.categoryService.nameAr(id) || null : null;
  });

  readonly priceValidationError = computed(() => {
    const min = parseNumberInput(this.minPriceDraft());
    const max = parseNumberInput(this.maxPriceDraft());

    if (min != null && max != null && min > max) {
      return 'أقل سعر يجب أن يكون أصغر من أعلى سعر';
    }

    return null;
  });

  readonly priceRangeLabel = computed(() => {
    const min = this.minPrice();
    const max = this.maxPrice();

    if (min == null && max == null) {
      return null;
    }

    if (min != null && max != null) {
      return `${min} - ${max} د.أ`;
    }

    return min != null ? `من ${min} د.أ` : `حتى ${max} د.أ`;
  });

  readonly discountLabel = computed(() => {
    const value = this.minimumDiscount();
    return value != null ? `خصم ${value}% فأكثر` : null;
  });

  readonly hasActiveFilters = computed(
    () =>
      this.categoryFilter() !== null ||
      this.minPrice() !== null ||
      this.maxPrice() !== null ||
      this.minimumDiscount() !== null ||
      !!this.city()
  );

  readonly filtersModalOpen = signal(false);

  private readonly priceInput$ = new Subject<void>();
  private readonly cityInput$ = new Subject<void>();

  constructor() {
    this.categoryService
      .getAll()
      .pipe(catchError(() => of<Category[]>([])))
      .subscribe();

    this.priceInput$
      .pipe(debounceTime(PRICE_CITY_DEBOUNCE_MS), takeUntilDestroyed())
      .subscribe(() => this.applyPriceFilter());

    this.cityInput$
      .pipe(debounceTime(PRICE_CITY_DEBOUNCE_MS), takeUntilDestroyed())
      .subscribe(() => this.applyCityFilter());

    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.searchTerm.set(params.get('q') ?? '');
      this.categoryFilter.set(params.get('categoryId'));
      this.minPrice.set(parseNumberParam(params.get('minPrice')));
      this.maxPrice.set(parseNumberParam(params.get('maxPrice')));
      this.minimumDiscount.set(parseNumberParam(params.get('minimumDiscount')));
      this.city.set(params.get('city'));
      this.sortBy.set(params.get('sortBy') ?? '');

      this.minPriceDraft.set(this.minPrice() != null ? String(this.minPrice()) : '');
      this.maxPriceDraft.set(this.maxPrice() != null ? String(this.maxPrice()) : '');
      this.cityDraft.set(this.city() ?? '');

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
        categoryId: this.categoryFilter(),
        city: this.city(),
        minimumDiscount: this.minimumDiscount(),
        minPrice: this.minPrice(),
        maxPrice: this.maxPrice(),
        sortBy: this.sortBy() || null,
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

  openFiltersModal(): void {
    this.filtersModalOpen.set(true);
  }

  closeFiltersModal(): void {
    this.filtersModalOpen.set(false);
  }

  selectCategory(id: string | null): void {
    this.updateQueryParams({ categoryId: id });
  }

  selectDiscount(value: number | null): void {
    this.updateQueryParams({ minimumDiscount: value });
  }

  onSortChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.updateQueryParams({ sortBy: value || null });
  }

  onMinPriceInput(event: Event): void {
    this.minPriceDraft.set((event.target as HTMLInputElement).value);
    this.priceInput$.next();
  }

  onMaxPriceInput(event: Event): void {
    this.maxPriceDraft.set((event.target as HTMLInputElement).value);
    this.priceInput$.next();
  }

  onCityInput(event: Event): void {
    this.cityDraft.set((event.target as HTMLInputElement).value);
    this.cityInput$.next();
  }

  clearCategory(): void {
    this.updateQueryParams({ categoryId: null });
  }

  clearPrice(): void {
    this.updateQueryParams({ minPrice: null, maxPrice: null });
  }

  clearDiscount(): void {
    this.updateQueryParams({ minimumDiscount: null });
  }

  clearCity(): void {
    this.updateQueryParams({ city: null });
  }

  clearAllFilters(): void {
    this.updateQueryParams({
      categoryId: null,
      minPrice: null,
      maxPrice: null,
      minimumDiscount: null,
      city: null,
      sortBy: null
    });
  }

  private applyPriceFilter(): void {
    if (this.priceValidationError()) {
      return;
    }

    this.updateQueryParams({
      minPrice: parseNumberInput(this.minPriceDraft()),
      maxPrice: parseNumberInput(this.maxPriceDraft())
    });
  }

  private applyCityFilter(): void {
    this.updateQueryParams({ city: this.cityDraft().trim() || null });
  }

  private updateQueryParams(patch: Record<string, string | number | null>): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: patch,
      queryParamsHandling: 'merge'
    });
  }
}
