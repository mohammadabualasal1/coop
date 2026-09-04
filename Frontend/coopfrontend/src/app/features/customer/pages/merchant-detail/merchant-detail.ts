import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { MerchantSummary, OfferSummary } from '../../../../core/models/marketplace.models';
import { AuthService } from '../../../../core/services/auth.service';
import { FollowService } from '../../../../core/services/follow.service';
import { MarketplaceService } from '../../../../core/services/marketplace.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiEmptyStateComponent,
  UiRatingStarsComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { OfferCardComponent } from '../../components/offer-card/offer-card';

type PageStatus = 'loading' | 'error' | 'notfound' | 'loaded';

@Component({
  selector: 'app-merchant-detail',
  imports: [
    RouterLink,
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

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly merchant = signal<MerchantSummary | null>(null);
  readonly offers = signal<OfferSummary[]>([]);

  readonly isFollowing = computed(() => {
    const merchant = this.merchant();
    return merchant ? this.followService.isFollowing(merchant.id) : false;
  });

  readonly followSaving = signal(false);

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      const id = params.get('id');

      if (id) {
        this.load(id);
      }
    });
  }

  load(id: string): void {
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
    const request$ = this.isFollowing()
      ? this.followService.unfollow(merchant.id)
      : this.followService.follow(merchant.id);

    request$.subscribe({
      next: () => this.followSaving.set(false),
      error: () => this.followSaving.set(false)
    });
  }
}
