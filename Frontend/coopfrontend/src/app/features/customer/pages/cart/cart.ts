import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { CartItem } from '../../../../core/models/cart.models';
import { CartService } from '../../../../core/services/cart.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiCardComponent,
  UiConfirmModalComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { JodPipe } from '../../../../shared/pipes/jod.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';
const UPDATE_DEBOUNCE_MS = 400;

@Component({
  selector: 'app-customer-cart',
  imports: [
    RouterLink,
    JodPipe,
    UiCardComponent,
    UiButtonComponent,
    UiAlertComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiConfirmModalComponent
  ],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CartComponent implements OnInit, OnDestroy {
  private readonly cartService = inject(CartService);
  private readonly router = inject(Router);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly actionErrorMessage = signal<string | null>(null);

  readonly cart = this.cartService.cart;
  readonly isEmpty = computed(() => {
    const cart = this.cart();
    return !cart || cart.id === EMPTY_GUID || cart.items.length === 0;
  });

  readonly pendingQuantities = signal<Record<string, number>>({});
  private readonly debounceTimers = new Map<string, ReturnType<typeof setTimeout>>();

  readonly confirmRemoveOpen = signal(false);
  readonly itemToRemove = signal<CartItem | null>(null);
  readonly removeSaving = signal(false);
  readonly removeErrorMessage = signal<string | null>(null);

  readonly confirmClearOpen = signal(false);
  readonly clearSaving = signal(false);
  readonly clearErrorMessage = signal<string | null>(null);

  readonly checkingOut = signal(false);
  readonly validationIssues = signal<string[]>([]);

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    for (const timer of this.debounceTimers.values()) {
      clearTimeout(timer);
    }
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.cartService.get().subscribe({
      next: () => this.status.set('loaded'),
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  displayQuantity(item: CartItem): number {
    return this.pendingQuantities()[item.id] ?? item.quantity;
  }

  displayLineTotal(item: CartItem): number {
    return item.unitPrice * this.displayQuantity(item);
  }

  changeQuantity(item: CartItem, delta: number): void {
    const next = Math.max(1, this.displayQuantity(item) + delta);

    this.pendingQuantities.update((map) => ({ ...map, [item.id]: next }));
    this.scheduleUpdate(item.id, next);
  }

  private scheduleUpdate(itemId: string, quantity: number): void {
    const existing = this.debounceTimers.get(itemId);

    if (existing) {
      clearTimeout(existing);
    }

    const timer = setTimeout(() => {
      this.debounceTimers.delete(itemId);

      this.cartService.updateItem(itemId, quantity).subscribe({
        next: () => this.clearPending(itemId),
        error: (err: unknown) => {
          this.clearPending(itemId);
          this.actionErrorMessage.set(extractErrorMessage(err));
        }
      });
    }, UPDATE_DEBOUNCE_MS);

    this.debounceTimers.set(itemId, timer);
  }

  private clearPending(itemId: string): void {
    this.pendingQuantities.update((map) => {
      const next = { ...map };
      delete next[itemId];
      return next;
    });
  }

  openRemoveConfirm(item: CartItem): void {
    this.itemToRemove.set(item);
    this.removeErrorMessage.set(null);
    this.confirmRemoveOpen.set(true);
  }

  closeRemoveConfirm(): void {
    if (this.removeSaving()) {
      return;
    }

    this.confirmRemoveOpen.set(false);
    this.itemToRemove.set(null);
  }

  confirmRemove(): void {
    const item = this.itemToRemove();

    if (!item || this.removeSaving()) {
      return;
    }

    this.removeSaving.set(true);
    this.removeErrorMessage.set(null);

    this.cartService.removeItem(item.id).subscribe({
      next: () => {
        this.removeSaving.set(false);
        this.confirmRemoveOpen.set(false);
        this.itemToRemove.set(null);
      },
      error: (err: unknown) => {
        this.removeSaving.set(false);
        this.removeErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openClearConfirm(): void {
    this.clearErrorMessage.set(null);
    this.confirmClearOpen.set(true);
  }

  closeClearConfirm(): void {
    if (this.clearSaving()) {
      return;
    }

    this.confirmClearOpen.set(false);
  }

  confirmClear(): void {
    if (this.clearSaving()) {
      return;
    }

    this.clearSaving.set(true);
    this.clearErrorMessage.set(null);

    this.cartService.clear().subscribe({
      next: () => {
        this.clearSaving.set(false);
        this.confirmClearOpen.set(false);
      },
      error: (err: unknown) => {
        this.clearSaving.set(false);
        this.clearErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  proceedToCheckout(): void {
    if (this.checkingOut()) {
      return;
    }

    this.checkingOut.set(true);
    this.actionErrorMessage.set(null);
    this.validationIssues.set([]);

    this.cartService.validate().subscribe({
      next: (result) => {
        this.checkingOut.set(false);

        if (result.isValid) {
          this.router.navigateByUrl('/shop/checkout');
        } else {
          this.validationIssues.set(result.issues);
        }
      },
      error: (err: unknown) => {
        this.checkingOut.set(false);
        this.actionErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
