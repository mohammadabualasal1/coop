import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';

import { Category } from '../../../../core/models/category.models';
import { OfferSummary, PagedResponse } from '../../../../core/models/marketplace.models';
import { CategoryService } from '../../../../core/services/category.service';
import { MarketplaceService } from '../../../../core/services/marketplace.service';
import {
  UiButtonComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { OfferCardComponent } from '../../components/offer-card/offer-card';

const LATEST_PAGE_SIZE = 12;
const NEARBY_RADIUS_KM = 10;

type NearbyStatus = 'idle' | 'loading' | 'granted' | 'denied';

function emptyPage(): PagedResponse<OfferSummary> {
  return { items: [], totalCount: 0, pageNumber: 1, pageSize: LATEST_PAGE_SIZE };
}

@Component({
  selector: 'app-customer-home',
  imports: [RouterLink, OfferCardComponent, UiEmptyStateComponent, UiButtonComponent, UiSpinnerComponent],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit {
  private readonly marketplace = inject(MarketplaceService);
  private readonly categoryService = inject(CategoryService);

  readonly cardCap = 8;

  readonly endingSoon = signal<OfferSummary[]>([]);
  readonly topDiscounts = signal<OfferSummary[]>([]);
  readonly latest = signal<OfferSummary[]>([]);
  readonly categories = signal<Category[]>([]);

  readonly endingSoonVisible = computed(() => this.endingSoon().slice(0, this.cardCap));
  readonly topDiscountsVisible = computed(() => this.topDiscounts().slice(0, this.cardCap));
  readonly latestVisible = computed(() => this.latest().slice(0, this.cardCap));

  readonly allEmpty = computed(
    () => this.endingSoon().length === 0 && this.topDiscounts().length === 0 && this.latest().length === 0
  );

  readonly nearbyStatus = signal<NearbyStatus>('idle');
  readonly nearby = signal<OfferSummary[]>([]);
  readonly nearbyVisible = computed(() => this.nearby().slice(0, this.cardCap));

  ngOnInit(): void {
    forkJoin({
      endingSoon: this.marketplace.endingSoon().pipe(catchError(() => of<OfferSummary[]>([]))),
      topDiscounts: this.marketplace.topDiscounts().pipe(catchError(() => of<OfferSummary[]>([]))),
      latest: this.marketplace
        .searchOffers({ pageNumber: 1, pageSize: LATEST_PAGE_SIZE })
        .pipe(catchError(() => of(emptyPage())))
    }).subscribe((result) => {
      this.endingSoon.set(result.endingSoon);
      this.topDiscounts.set(result.topDiscounts);
      this.latest.set(result.latest.items);
    });

    this.categoryService
      .getAll()
      .pipe(catchError(() => of<Category[]>([])))
      .subscribe((categories) => {
        this.categories.set(
          categories
            .filter((category) => category.parentCategoryId === null)
            .sort((a, b) => a.displayOrder - b.displayOrder)
        );
      });
  }

  requestNearby(): void {
    if (!navigator.geolocation) {
      this.nearbyStatus.set('denied');
      return;
    }

    this.nearbyStatus.set('loading');

    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.marketplace
          .nearbyOffers(position.coords.latitude, position.coords.longitude, NEARBY_RADIUS_KM)
          .pipe(catchError(() => of<OfferSummary[]>([])))
          .subscribe((offers) => {
            this.nearby.set(offers);
            this.nearbyStatus.set('granted');
          });
      },
      () => {
        this.nearbyStatus.set('denied');
      }
    );
  }
}
