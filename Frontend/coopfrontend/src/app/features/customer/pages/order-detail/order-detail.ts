import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';

import {
  DeliveryStatus,
  DeliveryStatusLabels,
  OrderStatus,
  OrderStatusLabels,
  OrderStatusTones
} from '../../../../core/enums';
import {
  CustomerOrderDetail,
  OrderStatusHistoryEntry
} from '../../../../core/models/customer-order.models';
import { CreateReviewRequest } from '../../../../core/models/review.models';
import { OrderService } from '../../../../core/services/order.service';
import { ReviewService } from '../../../../core/services/review.service';
import { TrackingHubService } from '../../../../core/services/tracking-hub.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  MapPickerComponent,
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiCodeModalComponent,
  UiConfirmModalComponent,
  UiFieldComponent,
  UiModalComponent,
  UiRatingStarsComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';
import { JodPipe } from '../../../../shared/pipes/jod.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';

@Component({
  selector: 'app-customer-order-detail',
  imports: [
    JodPipe,
    CoopDatePipe,
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiSpinnerComponent,
    UiFieldComponent,
    UiModalComponent,
    UiConfirmModalComponent,
    UiCodeModalComponent,
    UiRatingStarsComponent,
    MapPickerComponent
  ],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrderDetailComponent implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly orderService = inject(OrderService);
  private readonly reviewService = inject(ReviewService);
  private readonly trackingHub = inject(TrackingHubService);

  private orderId: string | null = null;

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly actionErrorMessage = signal<string | null>(null);

  readonly order = signal<CustomerOrderDetail | null>(null);

  readonly currentStatus = signal<OrderStatus | null>(null);
  readonly historyEntries = signal<OrderStatusHistoryEntry[]>([]);
  readonly deliveryStatus = signal<DeliveryStatus | null>(null);
  readonly driverName = signal<string | null>(null);
  readonly driverPhone = signal<string | null>(null);
  readonly vehicleType = signal<string | null>(null);
  readonly vehiclePlateNumber = signal<string | null>(null);
  readonly driverLatitude = signal<number | null>(null);
  readonly driverLongitude = signal<number | null>(null);

  readonly hasDriver = computed(() => this.driverName() !== null);

  readonly reviewSubmitted = signal(false);

  readonly showCancelAction = computed(() => {
    const value = this.currentStatus();
    return (
      value === OrderStatus.PendingPayment ||
      value === OrderStatus.PendingMerchantConfirmation ||
      value === OrderStatus.Accepted
    );
  });

  readonly showDeliveryCodeAction = computed(() => this.currentStatus() === OrderStatus.OutForDelivery);
  readonly showConfirmDeliveryAction = computed(() => this.currentStatus() === OrderStatus.Delivered);

  readonly showRateAction = computed(() => {
    const value = this.currentStatus();
    return (value === OrderStatus.Completed || value === OrderStatus.Delivered) && !this.reviewSubmitted();
  });

  readonly confirmCancelOpen = signal(false);
  readonly cancelSaving = signal(false);
  readonly cancelErrorMessage = signal<string | null>(null);
  readonly cancelReason = signal('');

  readonly deliveryCodeModalOpen = signal(false);
  readonly deliveryCodeLoading = signal(false);
  readonly deliveryCode = signal<string | null>(null);
  readonly deliveryCodeExpiresAt = signal<string | null>(null);

  readonly confirmingDelivery = signal(false);

  readonly reviewModalOpen = signal(false);
  readonly reviewSaving = signal(false);
  readonly reviewErrorMessage = signal<string | null>(null);
  readonly reviewMerchantRating = signal(0);
  readonly reviewDriverRating = signal(0);
  readonly reviewComment = signal('');
  readonly reviewSubmitDisabled = computed(() => this.reviewSaving() || this.reviewMerchantRating() === 0);

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      const id = params.get('id');

      if (id) {
        this.orderId = id;
        this.load(id);
      }
    });

    this.trackingHub.orderStatusChanged$.pipe(takeUntilDestroyed()).subscribe((event) => {
      if (event.orderId !== this.orderId) {
        return;
      }

      this.currentStatus.set(event.status);
      this.historyEntries.update((list) => [...list, { status: event.status, changedAt: event.changedAt }]);
      this.order.update((order) => (order ? { ...order, status: event.status } : order));
    });

    this.trackingHub.driverAssigned$.pipe(takeUntilDestroyed()).subscribe((event) => {
      if (event.orderId !== this.orderId) {
        return;
      }

      this.driverName.set(event.driverName);
      this.driverPhone.set(event.driverPhone);
      this.vehicleType.set(event.vehicleType);
      this.vehiclePlateNumber.set(event.vehiclePlateNumber);
    });

    this.trackingHub.deliveryStatusChanged$.pipe(takeUntilDestroyed()).subscribe((event) => {
      if (event.orderId !== this.orderId) {
        return;
      }

      this.deliveryStatus.set(event.deliveryStatus);
    });

    this.trackingHub.driverLocationUpdated$.pipe(takeUntilDestroyed()).subscribe((event) => {
      if (event.orderId !== this.orderId) {
        return;
      }

      this.driverLatitude.set(event.latitude);
      this.driverLongitude.set(event.longitude);
    });
  }

  ngOnDestroy(): void {
    if (this.orderId) {
      this.trackingHub.leaveOrder(this.orderId);
    }
  }

  load(id: string): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);
    this.actionErrorMessage.set(null);
    this.reviewSubmitted.set(false);

    forkJoin({
      order: this.orderService.getById(id),
      tracking: this.orderService.getTracking(id)
    }).subscribe({
      next: ({ order, tracking }) => {
        this.order.set(order);
        this.currentStatus.set(tracking.status);
        this.historyEntries.set(tracking.history);
        this.deliveryStatus.set(tracking.deliveryStatus ?? null);
        this.driverName.set(tracking.driverName ?? null);
        this.driverPhone.set(tracking.driverPhone ?? null);
        this.vehicleType.set(tracking.vehicleType ?? null);
        this.vehiclePlateNumber.set(tracking.vehiclePlateNumber ?? null);
        this.driverLatitude.set(tracking.driverLatitude ?? null);
        this.driverLongitude.set(tracking.driverLongitude ?? null);
        this.status.set('loaded');

        this.trackingHub.connect().then(() => this.trackingHub.joinOrder(id));
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  retry(): void {
    if (this.orderId) {
      this.load(this.orderId);
    }
  }

  statusLabel(status: OrderStatus): string {
    return OrderStatusLabels[status];
  }

  statusTone(status: OrderStatus) {
    return OrderStatusTones[status];
  }

  deliveryStatusLabel(status: DeliveryStatus): string {
    return DeliveryStatusLabels[status];
  }

  openCancelConfirm(): void {
    this.cancelReason.set('');
    this.cancelErrorMessage.set(null);
    this.confirmCancelOpen.set(true);
  }

  closeCancelConfirm(): void {
    if (this.cancelSaving()) {
      return;
    }

    this.confirmCancelOpen.set(false);
  }

  onCancelReasonInput(event: Event): void {
    this.cancelReason.set((event.target as HTMLTextAreaElement).value);
  }

  confirmCancel(): void {
    if (!this.orderId || this.cancelSaving()) {
      return;
    }

    this.cancelSaving.set(true);
    this.cancelErrorMessage.set(null);

    this.orderService.cancel(this.orderId, this.cancelReason().trim()).subscribe({
      next: () => {
        this.cancelSaving.set(false);
        this.confirmCancelOpen.set(false);
        this.load(this.orderId!);
      },
      error: (err: unknown) => {
        this.cancelSaving.set(false);
        this.cancelErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  showDeliveryCode(): void {
    if (!this.orderId || this.deliveryCodeLoading()) {
      return;
    }

    this.deliveryCodeLoading.set(true);
    this.actionErrorMessage.set(null);

    this.orderService.deliveryCode(this.orderId).subscribe({
      next: (result) => {
        this.deliveryCodeLoading.set(false);
        this.deliveryCode.set(result.code);
        this.deliveryCodeExpiresAt.set(result.expiresAt);
        this.deliveryCodeModalOpen.set(true);
      },
      error: (err: unknown) => {
        this.deliveryCodeLoading.set(false);
        this.actionErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  closeDeliveryCodeModal(): void {
    this.deliveryCodeModalOpen.set(false);
  }

  confirmReceipt(): void {
    if (!this.orderId || this.confirmingDelivery()) {
      return;
    }

    this.confirmingDelivery.set(true);
    this.actionErrorMessage.set(null);

    this.orderService.confirmDelivery(this.orderId).subscribe({
      next: () => {
        this.confirmingDelivery.set(false);
        this.load(this.orderId!);
      },
      error: (err: unknown) => {
        this.confirmingDelivery.set(false);
        this.actionErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openReviewModal(): void {
    this.reviewMerchantRating.set(0);
    this.reviewDriverRating.set(0);
    this.reviewComment.set('');
    this.reviewErrorMessage.set(null);
    this.reviewModalOpen.set(true);
  }

  closeReviewModal(): void {
    if (this.reviewSaving()) {
      return;
    }

    this.reviewModalOpen.set(false);
  }

  setMerchantRating(value: number): void {
    this.reviewMerchantRating.set(value);
  }

  setDriverRating(value: number): void {
    this.reviewDriverRating.set(value);
  }

  onReviewCommentInput(event: Event): void {
    this.reviewComment.set((event.target as HTMLTextAreaElement).value);
  }

  submitReview(): void {
    if (!this.orderId || this.reviewSubmitDisabled()) {
      return;
    }

    this.reviewSaving.set(true);
    this.reviewErrorMessage.set(null);

    const body: CreateReviewRequest = {
      orderId: this.orderId,
      merchantRating: this.reviewMerchantRating(),
      driverRating: this.hasDriver() && this.reviewDriverRating() > 0 ? this.reviewDriverRating() : null,
      comment: this.reviewComment().trim() ? this.reviewComment().trim() : null
    };

    this.reviewService.create(body).subscribe({
      next: () => {
        this.reviewSaving.set(false);
        this.reviewModalOpen.set(false);
        this.reviewSubmitted.set(true);
      },
      error: (err: unknown) => {
        this.reviewSaving.set(false);
        this.reviewErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
