import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { FavoriteOffer } from '../../../../core/models/marketplace.models';
import { FavoriteService } from '../../../../core/services/favorite.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { JodPipe } from '../../../../shared/pipes/jod.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';

@Component({
  selector: 'app-customer-favorites',
  imports: [RouterLink, JodPipe, UiAlertComponent, UiButtonComponent, UiEmptyStateComponent, UiSpinnerComponent],
  templateUrl: './favorites.html',
  styleUrl: './favorites.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FavoritesComponent implements OnInit {
  private readonly favoriteService = inject(FavoriteService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly favorites = signal<FavoriteOffer[]>([]);

  readonly removingOfferId = signal<string | null>(null);
  readonly removeErrorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.favoriteService.getAll().subscribe({
      next: (favorites) => {
        this.favorites.set(favorites);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  remove(offerId: string): void {
    if (this.removingOfferId()) {
      return;
    }

    this.removingOfferId.set(offerId);
    this.removeErrorMessage.set(null);

    this.favoriteService.remove(offerId).subscribe({
      next: () => {
        this.favorites.update((list) => list.filter((favorite) => favorite.offerId !== offerId));
        this.removingOfferId.set(null);
      },
      error: (err: unknown) => {
        this.removeErrorMessage.set(extractErrorMessage(err));
        this.removingOfferId.set(null);
      }
    });
  }
}
