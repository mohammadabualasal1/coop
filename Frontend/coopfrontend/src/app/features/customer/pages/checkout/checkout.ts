import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { PaymentMethod, PaymentMethodLabels } from '../../../../core/enums';
import { Address } from '../../../../core/models/address.models';
import { CheckoutCalculation } from '../../../../core/models/checkout.models';
import { PlaceOrderRequest } from '../../../../core/models/order.models';
import { AddressService } from '../../../../core/services/address.service';
import { CartService } from '../../../../core/services/cart.service';
import { CheckoutService } from '../../../../core/services/checkout.service';
import { OrderService } from '../../../../core/services/order.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { JodPipe } from '../../../../shared/pipes/jod.pipe';
import { AddressFormModalComponent } from '../../components/address-form-modal/address-form-modal';

type PageStatus = 'loading' | 'error' | 'loaded';

@Component({
  selector: 'app-customer-checkout',
  imports: [
    JodPipe,
    UiCardComponent,
    UiButtonComponent,
    UiAlertComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    AddressFormModalComponent
  ],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CheckoutComponent implements OnInit {
  private readonly addressService = inject(AddressService);
  private readonly cartService = inject(CartService);
  private readonly checkoutService = inject(CheckoutService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  readonly PaymentMethod = PaymentMethod;
  readonly paymentMethodLabels = PaymentMethodLabels;

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);

  readonly addresses = signal<Address[]>([]);
  readonly selectedAddressId = signal<string | null>(null);

  readonly paymentMethod = signal<PaymentMethod>(PaymentMethod.CashOnDelivery);
  readonly notes = signal('');

  readonly calculation = signal<CheckoutCalculation | null>(null);
  readonly calculating = signal(false);
  readonly calculateErrorMessage = signal<string | null>(null);

  readonly addressModalOpen = signal(false);

  readonly placingOrder = signal(false);
  readonly placeErrorMessage = signal<string | null>(null);
  readonly validationIssues = signal<string[]>([]);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.addressService.getAll().subscribe({
      next: (addresses) => {
        this.addresses.set(addresses);

        const preselected = addresses.find((a) => a.isDefault) ?? addresses[0] ?? null;

        if (preselected) {
          this.selectedAddressId.set(preselected.id);
          this.recalculate();
        }

        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  selectAddress(id: string): void {
    if (this.selectedAddressId() === id) {
      return;
    }

    this.selectedAddressId.set(id);
    this.recalculate();
  }

  selectPaymentMethod(method: PaymentMethod): void {
    this.paymentMethod.set(method);
  }

  onNotesInput(event: Event): void {
    this.notes.set((event.target as HTMLTextAreaElement).value);
  }

  private recalculate(): void {
    const addressId = this.selectedAddressId();

    if (!addressId) {
      return;
    }

    this.calculating.set(true);
    this.calculateErrorMessage.set(null);

    this.checkoutService.calculate({ customerAddressId: addressId }).subscribe({
      next: (result) => {
        this.calculating.set(false);
        this.calculation.set(result);
      },
      error: (err: unknown) => {
        this.calculating.set(false);
        this.calculateErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openAddAddressModal(): void {
    this.addressModalOpen.set(true);
  }

  closeAddressModal(): void {
    this.addressModalOpen.set(false);
  }

  onAddressSaved(address: Address): void {
    this.addressModalOpen.set(false);
    this.addresses.update((list) => [...list, address]);
    this.selectAddress(address.id);
  }

  placeOrder(): void {
    const addressId = this.selectedAddressId();

    if (!addressId || this.placingOrder()) {
      return;
    }

    this.placingOrder.set(true);
    this.placeErrorMessage.set(null);
    this.validationIssues.set([]);

    this.cartService.validate().subscribe({
      next: (validation) => {
        if (!validation.isValid) {
          this.placingOrder.set(false);
          this.validationIssues.set(validation.issues);
          return;
        }

        const body: PlaceOrderRequest = {
          customerAddressId: addressId,
          paymentMethod: this.paymentMethod(),
          customerNotes: this.notes().trim() ? this.notes().trim() : null
        };

        this.orderService.place(body).subscribe({
          next: (order) => {
            this.placingOrder.set(false);
            this.cartService.reset();
            this.router.navigateByUrl(`/shop/orders/${order.id}`);
          },
          error: (err: unknown) => {
            this.placingOrder.set(false);
            this.placeErrorMessage.set(extractErrorMessage(err));
          }
        });
      },
      error: (err: unknown) => {
        this.placingOrder.set(false);
        this.placeErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
