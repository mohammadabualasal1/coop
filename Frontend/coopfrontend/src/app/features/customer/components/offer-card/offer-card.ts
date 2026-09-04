import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { OfferSummary } from '../../../../core/models/marketplace.models';
import { AuthService } from '../../../../core/services/auth.service';
import { FavoriteService } from '../../../../core/services/favorite.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import { UiAlertComponent } from '../../../../shared/components';
import { DiscountPercentPipe } from '../../../../shared/pipes/discount-percent.pipe';
import { JodPipe } from '../../../../shared/pipes/jod.pipe';

const ENDING_SOON_HOURS = 24;
const MS_PER_HOUR = 3_600_000;

@Component({
  selector: 'app-offer-card',
  imports: [RouterLink, JodPipe, DiscountPercentPipe, UiAlertComponent],
  templateUrl: './offer-card.html',
  styleUrl: './offer-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OfferCardComponent {
  private readonly auth = inject(AuthService);
  private readonly favoriteService = inject(FavoriteService);
  private readonly router = inject(Router);

  readonly offer = input.required<OfferSummary>();

  readonly isFavorite = computed(() => this.favoriteService.isFavorite(this.offer().id));
  readonly favoriteSaving = signal(false);
  readonly favoriteErrorMessage = signal<string | null>(null);

  readonly hoursRemaining = computed<number | null>(() => {
    const endAt = new Date(this.offer().endAt).getTime();

    if (Number.isNaN(endAt)) {
      return null;
    }

    const hours = Math.ceil((endAt - Date.now()) / MS_PER_HOUR);
    return hours > 0 && hours <= ENDING_SOON_HOURS ? hours : null;
  });

  readonly distanceLabel = computed<string | null>(() => {
    const km = this.offer().distanceKm;
    return km !== undefined ? km.toFixed(1) : null;
  });

  toggleFavorite(event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    if (this.favoriteSaving()) {
      return;
    }

    this.favoriteSaving.set(true);
    this.favoriteErrorMessage.set(null);
    const offerId = this.offer().id;
    const request$: Observable<unknown> = this.isFavorite()
      ? this.favoriteService.remove(offerId)
      : this.favoriteService.add(offerId);

    request$.subscribe({
      next: () => this.favoriteSaving.set(false),
      error: (err: unknown) => {
        this.favoriteSaving.set(false);
        this.favoriteErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
