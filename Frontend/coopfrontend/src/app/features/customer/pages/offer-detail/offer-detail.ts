import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';

import { UserRole } from '../../../../core/enums';
import { OfferDetail } from '../../../../core/models/marketplace.models';
import { AuthService } from '../../../../core/services/auth.service';
import { CartService } from '../../../../core/services/cart.service';
import { MarketplaceService } from '../../../../core/services/marketplace.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  MapPickerComponent,
  UiAlertComponent,
  UiButtonComponent,
  UiConfirmModalComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';
import { DiscountPercentPipe } from '../../../../shared/pipes/discount-percent.pipe';
import { JodPipe } from '../../../../shared/pipes/jod.pipe';

type PageStatus = 'loading' | 'error' | 'notfound' | 'loaded';

const ENDING_SOON_HOURS = 24;
const MS_PER_HOUR = 3_600_000;

@Component({
  selector: 'app-offer-detail',
  imports: [
    RouterLink,
    JodPipe,
    CoopDatePipe,
    DiscountPercentPipe,
    UiAlertComponent,
    UiButtonComponent,
    UiConfirmModalComponent,
    UiEmptyStateComponent,
    UiSpinnerComponent,
    MapPickerComponent
  ],
  templateUrl: './offer-detail.html',
  styleUrl: './offer-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OfferDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly cartService = inject(CartService);
  private readonly marketplace = inject(MarketplaceService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly offer = signal<OfferDetail | null>(null);
  readonly quantity = signal(1);

  readonly addingToCart = signal(false);
  readonly addErrorMessage = signal<string | null>(null);
  readonly addSuccess = signal(false);

  readonly conflictModalOpen = signal(false);
  readonly conflictMessage = signal<string | null>(null);
  readonly conflictSaving = signal(false);

  readonly isGuest = computed(() => !this.auth.isAuthenticated());
  readonly isStaff = computed(
    () => this.auth.role() === UserRole.Merchant || this.auth.role() === UserRole.Admin
  );

  readonly totalAvailableStock = computed(() => {
    const offer = this.offer();
    return offer ? offer.branches.reduce((max, b) => Math.max(max, b.availableStock), 0) : 0;
  });

  readonly maxQuantity = computed(() => {
    const offer = this.offer();

    if (!offer) {
      return 1;
    }

    const stock = this.totalAvailableStock();
    const limit =
      offer.maximumQuantityPerCustomer != null
        ? Math.min(offer.maximumQuantityPerCustomer, stock)
        : stock;

    return Math.max(1, limit);
  });

  readonly outOfStock = computed(() => this.totalAvailableStock() === 0);

  readonly topStockBranch = computed(() => {
    const offer = this.offer();

    if (!offer || offer.branches.length === 0) {
      return null;
    }

    return offer.branches.reduce((best, branch) =>
      branch.availableStock > best.availableStock ? branch : best
    );
  });

  readonly addButtonDisabled = computed(
    () => this.outOfStock() || this.isStaff() || this.addingToCart()
  );

  readonly addButtonLabel = computed(() => (this.outOfStock() ? 'نفدت الكمية' : 'أضف إلى السلة'));

  readonly isEndingSoon = computed(() => {
    const offer = this.offer();

    if (!offer) {
      return false;
    }

    const endAt = new Date(offer.endAt).getTime();

    if (Number.isNaN(endAt)) {
      return false;
    }

    const hoursLeft = (endAt - Date.now()) / MS_PER_HOUR;
    return hoursLeft > 0 && hoursLeft <= ENDING_SOON_HOURS;
  });

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
    this.quantity.set(1);
    this.addSuccess.set(false);
    this.addErrorMessage.set(null);

    this.marketplace.getOffer(id).subscribe({
      next: (offer) => {
        this.offer.set(offer);
        this.quantity.set(1);
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

  increment(): void {
    this.quantity.update((value) => Math.min(this.maxQuantity(), value + 1));
  }

  decrement(): void {
    this.quantity.update((value) => Math.max(1, value - 1));
  }

  onAddToCart(): void {
    if (this.isGuest()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    if (this.addButtonDisabled()) {
      return;
    }

    this.performAdd();
  }

  private performAdd(): void {
    const offer = this.offer();

    if (!offer) {
      return;
    }

    this.addingToCart.set(true);
    this.addErrorMessage.set(null);
    this.addSuccess.set(false);

    this.cartService.addItem(offer.id, this.quantity()).subscribe({
      next: () => {
        this.addingToCart.set(false);
        this.addSuccess.set(true);
      },
      error: (err: unknown) => {
        this.addingToCart.set(false);

        if (err instanceof HttpErrorResponse && err.status === 400) {
          this.conflictMessage.set(extractErrorMessage(err));
          this.conflictModalOpen.set(true);
          return;
        }

        this.addErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  closeConflictModal(): void {
    if (this.conflictSaving()) {
      return;
    }

    this.conflictModalOpen.set(false);
    this.conflictMessage.set(null);
  }

  confirmConflict(): void {
    const offer = this.offer();

    if (!offer || this.conflictSaving()) {
      return;
    }

    this.conflictSaving.set(true);

    this.cartService
      .clear()
      .pipe(switchMap(() => this.cartService.addItem(offer.id, this.quantity())))
      .subscribe({
        next: () => {
          this.conflictSaving.set(false);
          this.conflictModalOpen.set(false);
          this.conflictMessage.set(null);
          this.addSuccess.set(true);
        },
        error: (err: unknown) => {
          this.conflictSaving.set(false);
          this.conflictModalOpen.set(false);
          this.conflictMessage.set(null);
          this.addErrorMessage.set(extractErrorMessage(err));
        }
      });
  }
}
