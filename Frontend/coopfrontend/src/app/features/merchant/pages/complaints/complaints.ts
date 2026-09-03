import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';

import { ComplaintStatus, ComplaintStatusLabels, ComplaintStatusTones } from '../../../../core/enums';
import { Complaint, CreateComplaintRequest } from '../../../../core/models/complaint.models';
import { MerchantOrder } from '../../../../core/models/order.models';
import { ComplaintService } from '../../../../core/services/complaint.service';
import { MerchantOrderService } from '../../../../core/services/merchant-order.service';
import { MerchantService } from '../../../../core/services/merchant.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiFieldComponent,
  UiModalComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';
type TargetMode = 'order' | 'general';

const SUCCESS_MESSAGE_MS = 4000;

function isValidUrl(value: string): boolean {
  if (!value) {
    return false;
  }

  try {
    new URL(value);
    return true;
  } catch {
    return false;
  }
}

function optionalUrlValidator(control: AbstractControl): ValidationErrors | null {
  const raw: unknown = control.value;
  const value = typeof raw === 'string' ? raw.trim() : '';
  return !value || isValidUrl(value) ? null : { url: true };
}

@Component({
  selector: 'app-merchant-complaints',
  imports: [
    ReactiveFormsModule,
    CoopDatePipe,
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiFieldComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiModalComponent
  ],
  templateUrl: './complaints.html',
  styleUrl: './complaints.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ComplaintsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly complaintService = inject(ComplaintService);
  private readonly merchantService = inject(MerchantService);
  private readonly orderService = inject(MerchantOrderService);

  readonly ComplaintStatus = ComplaintStatus;
  readonly allStatuses = [
    ComplaintStatus.Open,
    ComplaintStatus.UnderReview,
    ComplaintStatus.Resolved,
    ComplaintStatus.Rejected
  ];

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly complaints = signal<Complaint[]>([]);
  readonly selectedStatus = signal<ComplaintStatus | 'all'>('all');

  readonly orders = signal<MerchantOrder[]>([]);
  readonly merchantId = signal<string | null>(null);

  readonly successMessage = signal<string | null>(null);
  private successMessageTimeout: ReturnType<typeof setTimeout> | null = null;

  readonly createModalOpen = signal(false);
  readonly createSaving = signal(false);
  readonly createErrorMessage = signal<string | null>(null);
  readonly targetMode = signal<TargetMode>('general');
  readonly attemptedSubmit = signal(false);

  readonly createForm = this.fb.nonNullable.group({
    orderId: [''],
    category: ['', [Validators.required, Validators.minLength(2)]],
    description: ['', [Validators.required, Validators.minLength(10)]],
    evidenceUrl: ['', [optionalUrlValidator]]
  });

  ngOnInit(): void {
    this.load();
    this.loadOrders();
    this.ensureMerchantId();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    const selected = this.selectedStatus();

    this.complaintService.getMy(selected === 'all' ? null : selected).subscribe({
      next: (complaints) => {
        this.complaints.set(complaints);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  private loadOrders(): void {
    this.orderService.getAll().subscribe({
      next: (orders) => this.orders.set(orders),
      error: () => {}
    });
  }

  private ensureMerchantId(): void {
    const cached = this.merchantService.merchant();

    if (cached) {
      this.merchantId.set(cached.id);
      return;
    }

    this.merchantService.getMy().subscribe({
      next: (merchant) => this.merchantId.set(merchant.id),
      error: () => {}
    });
  }

  selectStatus(value: ComplaintStatus | 'all'): void {
    this.selectedStatus.set(value);
    this.load();
  }

  statusLabel(status: ComplaintStatus): string {
    return ComplaintStatusLabels[status];
  }

  statusTone(status: ComplaintStatus) {
    return ComplaintStatusTones[status];
  }

  openCreateModal(): void {
    this.createErrorMessage.set(null);
    this.attemptedSubmit.set(false);
    this.targetMode.set('general');
    this.createForm.reset({ orderId: '', category: '', description: '', evidenceUrl: '' });
    this.createModalOpen.set(true);
  }

  closeCreateModal(): void {
    if (this.createSaving()) {
      return;
    }

    this.createModalOpen.set(false);
  }

  setTargetMode(mode: TargetMode): void {
    this.targetMode.set(mode);
    this.createForm.controls.orderId.reset('');
  }

  orderErrorMessage(): string | null {
    return this.attemptedSubmit() && this.targetMode() === 'order' && !this.createForm.controls.orderId.value
      ? 'يجب اختيار طلب'
      : null;
  }

  categoryErrorMessage(): string | null {
    const control = this.createForm.controls.category;

    if (!control.invalid || !control.touched) {
      return null;
    }

    return 'التصنيف مطلوب';
  }

  descriptionErrorMessage(): string | null {
    const control = this.createForm.controls.description;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'الوصف مطلوب';
    }

    return 'الوصف يجب أن يكون 10 أحرف على الأقل';
  }

  evidenceUrlErrorMessage(): string | null {
    return this.createForm.controls.evidenceUrl.hasError('url') ? 'الرابط غير صالح' : null;
  }

  submitCreate(): void {
    if (this.createSaving()) {
      return;
    }

    const mode = this.targetMode();
    const raw = this.createForm.getRawValue();

    if (mode === 'order' && !raw.orderId) {
      this.attemptedSubmit.set(true);
      return;
    }

    if (this.createForm.invalid) {
      this.attemptedSubmit.set(true);
      this.createForm.markAllAsTouched();
      return;
    }

    const merchantId = this.merchantId();

    if (mode === 'general' && !merchantId) {
      this.createErrorMessage.set('يرجى الانتظار حتى يتم تحميل بيانات المتجر ثم إعادة المحاولة');
      return;
    }

    const body: CreateComplaintRequest = {
      category: raw.category,
      description: raw.description,
      evidenceUrl: raw.evidenceUrl.trim() ? raw.evidenceUrl : null,
      ...(mode === 'order' ? { orderId: raw.orderId } : { merchantId })
    };

    this.createSaving.set(true);
    this.createErrorMessage.set(null);

    this.complaintService.create(body).subscribe({
      next: () => {
        this.createSaving.set(false);
        this.createModalOpen.set(false);
        this.showSuccessMessage('تم تقديم الشكوى');
        this.load();
      },
      error: (err: unknown) => {
        this.createSaving.set(false);
        this.createErrorMessage.set(extractErrorMessage(err));
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
}
