import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';

import { FavoriteOffer, OfferSummary } from '../../../../core/models/marketplace.models';
import { FavoriteService } from '../../../../core/services/favorite.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { OfferCardComponent } from '../../components/offer-card/offer-card';

type PageStatus = 'loading' | 'error' | 'loaded';

@Component({
  selector: 'app-customer-favorites',
  imports: [UiAlertComponent, UiButtonComponent, UiEmptyStateComponent, UiSpinnerComponent, OfferCardComponent],
  templateUrl: './favorites.html',
  styleUrl: './favorites.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FavoritesComponent implements OnInit {
  private readonly favoriteService = inject(FavoriteService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  private readonly loadedFavorites = signal<FavoriteOffer[]>([]);

  // Filtering by the shared favorite-ids signal (rather than just showing
  // loadedFavorites as-is) means unfavoriting from the card's own heart button
  // removes the offer from this grid immediately, with no reload needed.
  readonly offers = computed<OfferSummary[]>(() =>
    this.loadedFavorites()
      .filter((favorite) => this.favoriteService.isFavorite(favorite.offerId))
      .map((favorite) => ({
        id: favorite.offerId,
        title: favorite.title,
        merchantId: favorite.merchantId,
        merchantName: favorite.merchantName,
        mainImageUrl: favorite.mainImageUrl,
        originalPrice: favorite.originalPrice,
        discountedPrice: favorite.discountedPrice,
        discountPercentage: favorite.discountPercentage,
        endAt: favorite.endAt
      }))
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.favoriteService.getAll().subscribe({
      next: (favorites) => {
        this.loadedFavorites.set(favorites);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }
}
