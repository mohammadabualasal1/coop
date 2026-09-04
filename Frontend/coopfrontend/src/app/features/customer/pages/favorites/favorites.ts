import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';

import { OfferSummary } from '../../../../core/models/marketplace.models';
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
  private readonly loadedOffers = signal<OfferSummary[]>([]);

  readonly offers = computed(() =>
    this.loadedOffers().filter((offer) => this.favoriteService.isFavorite(offer.id))
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.favoriteService.getAll().subscribe({
      next: (offers) => {
        this.loadedOffers.set(offers);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }
}
