import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { PendingOffer } from '../../../../core/models/admin.models';
import { AdminService } from '../../../../core/services/admin.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiConfirmModalComponent,
  UiEmptyStateComponent,
  UiFieldComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';
import { DiscountPercentPipe } from '../../../../shared/pipes/discount-percent.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';

const SUCCESS_MESSAGE_MS = 4000;

@Component({
  selector: 'app-admin-offers',
  imports: [
    ReactiveFormsModule,
    CoopDatePipe,
    DiscountPercentPipe,
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiFieldComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiConfirmModalComponent
  ],
  templateUrl: './offers.html',
  styleUrl: './offers.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OffersComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly pendingOffers = signal<PendingOffer[]>([]);
  readonly pendingCount = computed(() => this.pendingOffers().length);

  readonly successMessage = signal<string | null>(null);
  private successMessageTimeout: ReturnType<typeof setTimeout> | null = null;

  readonly confirmApproveOpen = signal(false);
  readonly offerToApprove = signal<PendingOffer | null>(null);
  readonly approveSaving = signal(false);
  readonly approveErrorMessage = signal<string | null>(null);
  readonly approveMessage = computed(() => {
    const offer = this.offerToApprove();
    return offer ? `الموافقة على العرض "${offer.title}"؟` : '';
  });

  readonly confirmRejectOpen = signal(false);
  readonly offerToReject = signal<PendingOffer | null>(null);
  readonly rejectSaving = signal(false);
  readonly rejectErrorMessage = signal<string | null>(null);
  readonly rejectMessage = computed(() => {
    const offer = this.offerToReject();
    return offer ? `رفض العرض "${offer.title}"؟` : '';
  });

  readonly rejectForm = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.minLength(5)]]
  });

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    if (this.successMessageTimeout) {
      clearTimeout(this.successMessageTimeout);
    }
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.adminService.getPendingOffers().subscribe({
      next: (offers) => {
        this.pendingOffers.set(offers);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  private showSuccessMessage(message: string): void {
    if (this.successMessageTimeout) {
      clearTimeout(this.successMessageTimeout);
    }

    this.successMessage.set(message);
    this.successMessageTimeout = setTimeout(() => {
      this.successMessage.set(null);
      this.successMessageTimeout = null;
    }, SUCCESS_MESSAGE_MS);
  }

  private removeOffer(id: string): void {
    this.pendingOffers.update((offers) => offers.filter((offer) => offer.id !== id));
  }

  openApproveConfirm(offer: PendingOffer): void {
    this.offerToApprove.set(offer);
    this.approveErrorMessage.set(null);
    this.confirmApproveOpen.set(true);
  }

  closeApproveConfirm(): void {
    if (this.approveSaving()) {
      return;
    }

    this.confirmApproveOpen.set(false);
    this.offerToApprove.set(null);
  }

  confirmApprove(): void {
    const offer = this.offerToApprove();

    if (!offer || this.approveSaving()) {
      return;
    }

    this.approveSaving.set(true);
    this.approveErrorMessage.set(null);

    this.adminService.approveOffer(offer.id).subscribe({
      next: () => {
        this.approveSaving.set(false);
        this.confirmApproveOpen.set(false);
        this.offerToApprove.set(null);
        this.removeOffer(offer.id);
        this.showSuccessMessage('تمت الموافقة على العرض');
      },
      error: (err: unknown) => {
        this.approveSaving.set(false);
        this.approveErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openRejectConfirm(offer: PendingOffer): void {
    this.offerToReject.set(offer);
    this.rejectErrorMessage.set(null);
    this.rejectForm.reset({ reason: '' });
    this.confirmRejectOpen.set(true);
  }

  closeRejectConfirm(): void {
    if (this.rejectSaving()) {
      return;
    }

    this.confirmRejectOpen.set(false);
    this.offerToReject.set(null);
  }

  reasonErrorMessage(): string | null {
    const control = this.rejectForm.controls.reason;
    return control.invalid && control.touched ? 'سبب الرفض مطلوب ولا يقل عن 5 أحرف' : null;
  }

  confirmReject(): void {
    const offer = this.offerToReject();

    if (!offer || this.rejectForm.invalid || this.rejectSaving()) {
      this.rejectForm.markAllAsTouched();
      return;
    }

    const raw = this.rejectForm.getRawValue();

    this.rejectSaving.set(true);
    this.rejectErrorMessage.set(null);

    this.adminService.rejectOffer(offer.id, raw.reason).subscribe({
      next: () => {
        this.rejectSaving.set(false);
        this.confirmRejectOpen.set(false);
        this.offerToReject.set(null);
        this.removeOffer(offer.id);
        this.showSuccessMessage('تم رفض العرض');
      },
      error: (err: unknown) => {
        this.rejectSaving.set(false);
        this.rejectErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
