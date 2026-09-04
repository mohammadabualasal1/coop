import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, forkJoin } from 'rxjs';

import { MerchantSummary, OfferSummary } from '../../../../core/models/marketplace.models';
import { Review } from '../../../../core/models/review.models';
import { AuthService } from '../../../../core/services/auth.service';
import { FollowService } from '../../../../core/services/follow.service';
import { MarketplaceService } from '../../../../core/services/marketplace.service';
import { ReviewService } from '../../../../core/services/review.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiEmptyStateComponent,
  UiRatingStarsComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';
import { OfferCardComponent } from '../../components/offer-card/offer-card';

type PageStatus = 'loading' | 'error' | 'notfound' | 'loaded';
type ReviewsStatus = 'loading' | 'error' | 'loaded';

const REVIEWS_PREVIEW_SIZE = 5;
const REVIEWS_FULL_PAGE_SIZE = 10;

@Component({
  selector: 'app-merchant-detail',
  imports: [
    RouterLink,
    CoopDatePipe,
    UiAlertComponent,
    UiButtonComponent,
    UiEmptyStateComponent,
    UiRatingStarsComponent,
    UiSpinnerComponent,
    OfferCardComponent
  ],
  templateUrl: './merchant-detail.html',
  styleUrl: './merchant-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MerchantDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly followService = inject(FollowService);
  private readonly marketplace = inject(MarketplaceService);
  private readonly reviewService = inject(ReviewService);

  private merchantId: string | null = null;

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly merchant = signal<MerchantSummary | null>(null);
  readonly offers = signal<OfferSummary[]>([]);

  readonly reviewsStatus = signal<ReviewsStatus>('loading');
  readonly reviewsErrorMessage = signal<string | null>(null);
  readonly reviews = signal<Review[]>([]);
  readonly reviewsExpanded = signal(false);
  readonly reviewsPageNumber = signal(1);
  readonly reviewsHasNextPage = computed(() => this.reviews().length === REVIEWS_FULL_PAGE_SIZE);

  readonly isFollowing = computed(() => {
    const merchant = this.merchant();
    return merchant ? this.followService.isFollowing(merchant.id) : false;
  });

  readonly followSaving = signal(false);
  readonly followErrorMessage = signal<string | null>(null);

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      const id = params.get('id');

      if (id) {
        this.load(id);
      }
    });
  }

  load(id: string): void {
    this.merchantId = id;
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    forkJoin({
      merchant: this.marketplace.getMerchant(id),
      offers: this.marketplace.getMerchantOffers(id)
    }).subscribe({
      next: ({ merchant, offers }) => {
        this.merchant.set(merchant);
        this.offers.set(offers);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        if (err instanceof HttpErrorResponse && err.status === 404) {
          this.status.set('notfound');
          return;
        }

        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });

    this.reviewsExpanded.set(false);
    this.reviewsPageNumber.set(1);
    this.loadReviews();
  }

  private loadReviews(): void {
    const id = this.merchantId;

    if (!id) {
      return;
    }

    this.reviewsStatus.set('loading');
    this.reviewsErrorMessage.set(null);

    const pageSize = this.reviewsExpanded() ? REVIEWS_FULL_PAGE_SIZE : REVIEWS_PREVIEW_SIZE;

    this.reviewService.getForMerchant(id, this.reviewsPageNumber(), pageSize).subscribe({
      next: (reviews) => {
        this.reviews.set(reviews);
        this.reviewsStatus.set('loaded');
      },
      error: (err: unknown) => {
        this.reviewsErrorMessage.set(extractErrorMessage(err));
        this.reviewsStatus.set('error');
      }
    });
  }

  expandReviews(): void {
    if (this.reviewsExpanded()) {
      return;
    }

    this.reviewsExpanded.set(true);
    this.reviewsPageNumber.set(1);
    this.loadReviews();
  }

  reviewsGoToPreviousPage(): void {
    if (this.reviewsPageNumber() <= 1) {
      return;
    }

    this.reviewsPageNumber.update((value) => value - 1);
    this.loadReviews();
  }

  reviewsGoToNextPage(): void {
    if (!this.reviewsHasNextPage()) {
      return;
    }

    this.reviewsPageNumber.update((value) => value + 1);
    this.loadReviews();
  }

  retry(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.load(id);
    }
  }

  toggleFollow(): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    const merchant = this.merchant();

    if (!merchant || this.followSaving()) {
      return;
    }

    this.followSaving.set(true);
    this.followErrorMessage.set(null);
    const request$: Observable<unknown> = this.isFollowing()
      ? this.followService.unfollow(merchant.id)
      : this.followService.follow(merchant.id);

    request$.subscribe({
      next: () => this.followSaving.set(false),
      error: (err: unknown) => {
        this.followSaving.set(false);
        this.followErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
