import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  Subject,
  catchError,
  filter,
  fromEvent,
  interval,
  map,
  merge,
  of,
  startWith,
  switchMap,
  tap
} from 'rxjs';

import { OrderStatus, OrderStatusLabels, OrderStatusTones, PaymentMethodLabels } from '../../../../core/enums';
import { MerchantOrderDetail, MerchantOrderSummary } from '../../../../core/models/order.models';
import { MerchantOrderService } from '../../../../core/services/merchant-order.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiCodeModalComponent,
  UiConfirmModalComponent,
  UiEmptyStateComponent,
  UiFieldComponent,
  UiModalComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';
import { JodPipe } from '../../../../shared/pipes/jod.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';
type ModalStatus = 'loading' | 'error' | 'loaded';
type OrderAction = 'details' | 'accept' | 'reject' | 'ready' | 'pickup-code';

const POLL_MS = 30000;

function orderActionsForStatus(status: OrderStatus): OrderAction[] {
  switch (status) {
    case OrderStatus.PendingMerchantConfirmation:
      return ['details', 'accept', 'reject'];
    case OrderStatus.Accepted:
    case OrderStatus.Preparing:
      return ['details', 'ready'];
    case OrderStatus.ReadyForPickup:
      return ['details', 'pickup-code'];
    default:
      return ['details'];
  }
}

@Component({
  selector: 'app-merchant-orders',
  imports: [
    ReactiveFormsModule,
    JodPipe,
    CoopDatePipe,
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiFieldComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiModalComponent,
    UiConfirmModalComponent,
    UiCodeModalComponent
  ],
  templateUrl: './orders.html',
  styleUrl: './orders.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrdersComponent {
  private readonly fb = inject(FormBuilder);
  private readonly orderService = inject(MerchantOrderService);

  readonly OrderStatus = OrderStatus;
  readonly allStatuses: OrderStatus[] = [
    OrderStatus.PendingMerchantConfirmation,
    OrderStatus.Accepted,
    OrderStatus.Rejected,
    OrderStatus.Preparing,
    OrderStatus.ReadyForPickup,
    OrderStatus.DriverAssigned,
    OrderStatus.OutForDelivery,
    OrderStatus.Delivered,
    OrderStatus.Completed,
    OrderStatus.Cancelled,
    OrderStatus.DeliveryFailed
  ];

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  // Set when a background poll/refresh fails after data is already on screen —
  // kept separate from loadErrorMessage so a transient outage doesn't blank the
  // list the merchant is already looking at, while still never staying silent.
  readonly refreshErrorMessage = signal<string | null>(null);
  readonly orders = signal<MerchantOrderSummary[]>([]);
  readonly selectedStatus = signal<OrderStatus | 'all'>('all');
  readonly actionErrorMessage = signal<string | null>(null);

  readonly detailsModalOpen = signal(false);
  readonly detailsOrder = signal<MerchantOrderDetail | null>(null);
  readonly detailsStatus = signal<ModalStatus>('loading');
  readonly detailsErrorMessage = signal<string | null>(null);

  readonly confirmAcceptOpen = signal(false);
  readonly orderToAccept = signal<MerchantOrderSummary | null>(null);
  readonly acceptSaving = signal(false);
  readonly acceptErrorMessage = signal<string | null>(null);
  readonly acceptMessage = computed(() => {
    const order = this.orderToAccept();
    return order ? `قبول الطلب ${order.orderNumber}؟` : '';
  });

  readonly confirmRejectOpen = signal(false);
  readonly orderToReject = signal<MerchantOrderSummary | null>(null);
  readonly rejectSaving = signal(false);
  readonly rejectErrorMessage = signal<string | null>(null);
  readonly rejectMessage = computed(() => {
    const order = this.orderToReject();
    return order ? `رفض الطلب ${order.orderNumber}؟` : '';
  });

  readonly rejectForm = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.minLength(5)]]
  });

  readonly markingReadyId = signal<string | null>(null);
  readonly reissuingId = signal<string | null>(null);

  readonly pickupCodeModalOpen = signal(false);
  readonly pickupCode = signal<string | null>(null);
  readonly pickupCodeExpiresAt = signal<string | null>(null);
  readonly pickupCodeIsReissue = signal(false);

  private readonly modalOpen = computed(
    () =>
      this.detailsModalOpen() ||
      this.confirmAcceptOpen() ||
      this.confirmRejectOpen() ||
      this.pickupCodeModalOpen()
  );

  private readonly reload$ = new Subject<boolean>();

  constructor() {
    merge(
      interval(POLL_MS).pipe(
        startWith(0),
        map(() => false)
      ),
      fromEvent(window, 'focus').pipe(map(() => false)),
      this.reload$
    )
      .pipe(
        filter(() => !this.modalOpen()),
        tap((showLoading) => {
          if (showLoading) {
            this.status.set('loading');
            this.loadErrorMessage.set(null);
          }
        }),
        switchMap(() => {
          const selected = this.selectedStatus();
          return this.orderService
            .getAll(selected === 'all' ? null : selected)
            .pipe(catchError((error: unknown) => of({ error })));
        }),
        takeUntilDestroyed()
      )
      .subscribe((result) => {
        if (Array.isArray(result)) {
          this.orders.set(result);
          this.status.set('loaded');
          this.refreshErrorMessage.set(null);
        } else if (this.status() === 'loaded') {
          this.refreshErrorMessage.set(extractErrorMessage(result.error));
        } else {
          this.loadErrorMessage.set(extractErrorMessage(result.error));
          this.status.set('error');
        }
      });
  }

  load(): void {
    this.reload$.next(true);
  }

  selectStatus(value: OrderStatus | 'all'): void {
    this.selectedStatus.set(value);
    this.load();
  }

  statusLabel(status: OrderStatus): string {
    return OrderStatusLabels[status];
  }

  statusTone(status: OrderStatus) {
    return OrderStatusTones[status];
  }

  paymentMethodLabel(method: MerchantOrderDetail['paymentMethod']): string {
    return PaymentMethodLabels[method];
  }

  orderActions(order: MerchantOrderSummary): OrderAction[] {
    return orderActionsForStatus(order.status);
  }

  openDetailsModal(order: MerchantOrderSummary): void {
    this.detailsModalOpen.set(true);
    this.detailsStatus.set('loading');
    this.detailsErrorMessage.set(null);

    this.orderService.getById(order.id).subscribe({
      next: (detail) => {
        this.detailsOrder.set(detail);
        this.detailsStatus.set('loaded');
      },
      error: (err: unknown) => {
        this.detailsErrorMessage.set(extractErrorMessage(err));
        this.detailsStatus.set('error');
      }
    });
  }

  closeDetailsModal(): void {
    this.detailsModalOpen.set(false);
    this.detailsOrder.set(null);
  }

  openAcceptConfirm(order: MerchantOrderSummary): void {
    this.orderToAccept.set(order);
    this.acceptErrorMessage.set(null);
    this.confirmAcceptOpen.set(true);
  }

  closeAcceptConfirm(): void {
    if (this.acceptSaving()) {
      return;
    }

    this.confirmAcceptOpen.set(false);
    this.orderToAccept.set(null);
  }

  confirmAccept(): void {
    const order = this.orderToAccept();

    if (!order || this.acceptSaving()) {
      return;
    }

    this.acceptSaving.set(true);
    this.acceptErrorMessage.set(null);

    this.orderService.accept(order.id).subscribe({
      next: () => {
        this.acceptSaving.set(false);
        this.confirmAcceptOpen.set(false);
        this.orderToAccept.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.acceptSaving.set(false);
        this.acceptErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openRejectConfirm(order: MerchantOrderSummary): void {
    this.orderToReject.set(order);
    this.rejectErrorMessage.set(null);
    this.rejectForm.reset({ reason: '' });
    this.confirmRejectOpen.set(true);
  }

  closeRejectConfirm(): void {
    if (this.rejectSaving()) {
      return;
    }

    this.confirmRejectOpen.set(false);
    this.orderToReject.set(null);
  }

  reasonErrorMessage(): string | null {
    const control = this.rejectForm.controls.reason;
    return control.invalid && control.touched ? 'سبب الرفض مطلوب ولا يقل عن 5 أحرف' : null;
  }

  confirmReject(): void {
    const order = this.orderToReject();

    if (!order || this.rejectForm.invalid || this.rejectSaving()) {
      this.rejectForm.markAllAsTouched();
      return;
    }

    const raw = this.rejectForm.getRawValue();

    this.rejectSaving.set(true);
    this.rejectErrorMessage.set(null);

    this.orderService.reject(order.id, raw.reason).subscribe({
      next: () => {
        this.rejectSaving.set(false);
        this.confirmRejectOpen.set(false);
        this.orderToReject.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.rejectSaving.set(false);
        this.rejectErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  markReady(order: MerchantOrderSummary): void {
    if (this.markingReadyId()) {
      return;
    }

    this.actionErrorMessage.set(null);
    this.markingReadyId.set(order.id);

    this.orderService.markReady(order.id).subscribe({
      next: (response) => {
        this.markingReadyId.set(null);

        if (response.pickupCode) {
          this.openPickupCodeModal(response.pickupCode, response.pickupCodeExpiresAt, false);
        } else {
          this.actionErrorMessage.set('لا توجد مهمة توصيل لهذا الطلب بعد، تعذّر إنشاء رمز الاستلام');
        }
      },
      error: (err: unknown) => {
        this.markingReadyId.set(null);
        this.actionErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  reissuePickupCode(order: MerchantOrderSummary): void {
    if (this.reissuingId()) {
      return;
    }

    this.actionErrorMessage.set(null);
    this.reissuingId.set(order.id);

    this.orderService.reissuePickupCode(order.id).subscribe({
      next: (response) => {
        this.reissuingId.set(null);
        this.openPickupCodeModal(response.code, response.expiresAt, true);
      },
      error: (err: unknown) => {
        this.reissuingId.set(null);
        this.actionErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  private openPickupCodeModal(code: string, expiresAt: string | null, isReissue: boolean): void {
    this.pickupCode.set(code);
    this.pickupCodeExpiresAt.set(expiresAt);
    this.pickupCodeIsReissue.set(isReissue);
    this.pickupCodeModalOpen.set(true);
  }

  closePickupCodeModal(): void {
    this.pickupCodeModalOpen.set(false);
    this.pickupCode.set(null);
    this.pickupCodeExpiresAt.set(null);
    this.pickupCodeIsReissue.set(false);
    this.load();
  }
}
