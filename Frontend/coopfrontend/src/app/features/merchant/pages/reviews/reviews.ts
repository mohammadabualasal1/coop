import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Observable, of, switchMap } from 'rxjs';

import { Merchant } from '../../../../core/models/merchant.models';
import { Review } from '../../../../core/models/review.models';
import { MerchantService } from '../../../../core/services/merchant.service';
import { ReviewService } from '../../../../core/services/review.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiRatingStarsComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-merchant-reviews',
  imports: [
    CoopDatePipe,
    UiCardComponent,
    UiButtonComponent,
    UiAlertComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiRatingStarsComponent
  ],
  templateUrl: './reviews.html',
  styleUrl: './reviews.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReviewsComponent implements OnInit {
  private readonly merchantService = inject(MerchantService);
  private readonly reviewService = inject(ReviewService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly merchant = signal<Merchant | null>(null);
  readonly reviews = signal<Review[]>([]);
  readonly pageNumber = signal(1);
  readonly hasNextPage = computed(() => this.reviews().length === PAGE_SIZE);

  ngOnInit(): void {
    this.load();
  }

  private ensureMerchant(): Observable<Merchant> {
    const cached = this.merchantService.merchant();
    return cached ? of(cached) : this.merchantService.getMy();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.ensureMerchant()
      .pipe(
        switchMap((merchant) => {
          this.merchant.set(merchant);
          return this.reviewService.getForMerchant(merchant.id, this.pageNumber(), PAGE_SIZE);
        })
      )
      .subscribe({
        next: (reviews) => {
          this.reviews.set(reviews);
          this.status.set('loaded');
        },
        error: (err: unknown) => {
          this.loadErrorMessage.set(extractErrorMessage(err));
          this.status.set('error');
        }
      });
  }

  averageRatingDisplay(): string {
    const merchant = this.merchant();
    return merchant?.averageRating != null ? merchant.averageRating.toFixed(1) : '0.0';
  }

  goToPreviousPage(): void {
    if (this.pageNumber() <= 1) {
      return;
    }

    this.pageNumber.update((value) => value - 1);
    this.load();
  }

  goToNextPage(): void {
    if (!this.hasNextPage()) {
      return;
    }

    this.pageNumber.update((value) => value + 1);
    this.load();
  }
}
