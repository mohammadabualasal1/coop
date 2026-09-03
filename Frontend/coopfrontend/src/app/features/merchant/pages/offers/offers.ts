import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { OfferStatus, OfferStatusLabels, OfferStatusTones } from '../../../../core/enums';
import { Offer, OfferRequest } from '../../../../core/models/offer.models';
import { Product } from '../../../../core/models/product.models';
import { OfferService } from '../../../../core/services/offer.service';
import { ProductService } from '../../../../core/services/product.service';
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
import { JodPipe } from '../../../../shared/pipes/jod.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';
type OfferAction = 'edit' | 'stock' | 'submit' | 'delete' | 'pause' | 'resume' | 'cancel';

function offerActionsForStatus(status: OfferStatus): OfferAction[] {
  switch (status) {
    case OfferStatus.Draft:
      return ['edit', 'stock', 'submit', 'delete'];
    case OfferStatus.Rejected:
      return ['edit', 'stock', 'submit'];
    case OfferStatus.PendingApproval:
      return ['stock'];
    case OfferStatus.Approved:
      return ['stock'];
    case OfferStatus.Scheduled:
      return ['stock', 'pause', 'cancel'];
    case OfferStatus.Active:
      return ['stock', 'pause', 'cancel'];
    case OfferStatus.Paused:
      return ['stock', 'resume', 'cancel'];
    case OfferStatus.Expired:
      return ['stock'];
    case OfferStatus.SoldOut:
      return ['stock'];
    case OfferStatus.Cancelled:
      return [];
  }
}

function toDatetimeLocalValue(iso: string): string {
  const date = new Date(iso);
  const pad = (value: number) => value.toString().padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function toIsoUtc(localValue: string): string {
  return new Date(localValue).toISOString();
}

function offerFormGroupValidator(group: AbstractControl): ValidationErrors | null {
  const originalPrice = group.get('originalPrice')?.value as number | null;
  const discountedPrice = group.get('discountedPrice')?.value as number | null;
  const startAt = group.get('startAt')?.value as string;
  const endAt = group.get('endAt')?.value as string;

  const errors: ValidationErrors = {};

  if (originalPrice != null && discountedPrice != null && !(discountedPrice < originalPrice)) {
    errors['discountTooHigh'] = true;
  }

  if (startAt && endAt && !(new Date(startAt).getTime() < new Date(endAt).getTime())) {
    errors['startAfterEnd'] = true;
  }

  if (endAt && !(new Date(endAt).getTime() > Date.now())) {
    errors['endNotFuture'] = true;
  }

  return Object.keys(errors).length > 0 ? errors : null;
}

@Component({
  selector: 'app-merchant-offers',
  imports: [
    ReactiveFormsModule,
    JodPipe,
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiFieldComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiModalComponent
  ],
  templateUrl: './offers.html',
  styleUrl: './offers.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OffersComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly offerService = inject(OfferService);
  private readonly productService = inject(ProductService);

  readonly OfferStatus = OfferStatus;

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly offers = signal<Offer[]>([]);
  readonly products = signal<Product[]>([]);

  readonly actionErrorMessage = signal<string | null>(null);
  readonly submittingId = signal<string | null>(null);
  readonly pausingId = signal<string | null>(null);
  readonly resumingId = signal<string | null>(null);

  readonly selectedStatus = signal<OfferStatus | 'all'>('all');

  readonly presentStatuses = computed(() => {
    const seen = new Set(this.offers().map((offer) => offer.status));
    return Array.from(seen).sort((a, b) => a - b);
  });

  readonly filteredOffers = computed(() => {
    const selected = this.selectedStatus();
    return selected === 'all' ? this.offers() : this.offers().filter((offer) => offer.status === selected);
  });

  readonly formModalOpen = signal(false);
  readonly editingOffer = signal<Offer | null>(null);
  readonly formSaving = signal(false);
  readonly formErrorMessage = signal<string | null>(null);
  readonly modalTitle = computed(() => (this.editingOffer() ? 'تعديل العرض' : 'إضافة عرض'));

  readonly confirmDeleteOpen = signal(false);
  readonly offerToDelete = signal<Offer | null>(null);
  readonly deleteSaving = signal(false);
  readonly deleteErrorMessage = signal<string | null>(null);

  readonly confirmCancelOpen = signal(false);
  readonly offerToCancel = signal<Offer | null>(null);
  readonly cancelSaving = signal(false);
  readonly cancelErrorMessage = signal<string | null>(null);

  readonly form = this.fb.group(
    {
      productId: this.fb.nonNullable.control('', [Validators.required]),
      title: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(3)]),
      description: this.fb.nonNullable.control(''),
      originalPrice: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0.01)]),
      discountedPrice: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0.01)]),
      startAt: this.fb.nonNullable.control('', [Validators.required]),
      endAt: this.fb.nonNullable.control('', [Validators.required]),
      maximumQuantityPerCustomer: this.fb.control<number | null>(null, [Validators.min(1)])
    },
    { validators: offerFormGroupValidator }
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    forkJoin({
      offers: this.offerService.getMy(),
      products: this.productService.getMy()
    }).subscribe({
      next: ({ offers, products }) => {
        this.offers.set(offers);
        this.products.set(products);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  statusLabel(status: OfferStatus): string {
    return OfferStatusLabels[status];
  }

  statusTone(status: OfferStatus) {
    return OfferStatusTones[status];
  }

  offerActions(offer: Offer): OfferAction[] {
    return offerActionsForStatus(offer.status);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-JO');
  }

  discountPercentagePreview(): number | null {
    const original = this.form.controls.originalPrice.value;
    const discounted = this.form.controls.discountedPrice.value;

    if (!original || original <= 0 || discounted == null) {
      return null;
    }

    return Math.round(((original - discounted) / original) * 1000) / 10;
  }

  openStockManagement(_offer: Offer): void {}

  openCreateModal(): void {
    this.editingOffer.set(null);
    this.formErrorMessage.set(null);
    this.form.reset({
      productId: '',
      title: '',
      description: '',
      originalPrice: 0,
      discountedPrice: 0,
      startAt: '',
      endAt: '',
      maximumQuantityPerCustomer: null
    });
    this.formModalOpen.set(true);
  }

  openEditModal(offer: Offer): void {
    if (!this.offerActions(offer).includes('edit')) {
      return;
    }

    this.editingOffer.set(offer);
    this.formErrorMessage.set(null);
    this.form.reset({
      productId: offer.productId,
      title: offer.title,
      description: offer.description ?? '',
      originalPrice: offer.originalPrice,
      discountedPrice: offer.discountedPrice,
      startAt: toDatetimeLocalValue(offer.startAt),
      endAt: toDatetimeLocalValue(offer.endAt),
      maximumQuantityPerCustomer: offer.maximumQuantityPerCustomer
    });
    this.formModalOpen.set(true);
  }

  closeFormModal(): void {
    if (this.formSaving()) {
      return;
    }

    this.formModalOpen.set(false);
  }

  productIdErrorMessage(): string | null {
    const control = this.form.controls.productId;
    return control.invalid && control.touched ? 'المنتج مطلوب' : null;
  }

  titleErrorMessage(): string | null {
    const control = this.form.controls.title;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'عنوان العرض مطلوب';
    }

    if (control.hasError('minlength')) {
      return 'عنوان العرض قصير جداً';
    }

    return null;
  }

  originalPriceErrorMessage(): string | null {
    const control = this.form.controls.originalPrice;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'السعر الأصلي مطلوب';
    }

    return 'السعر الأصلي يجب أن يكون 0.01 على الأقل';
  }

  discountedPriceErrorMessage(): string | null {
    const control = this.form.controls.discountedPrice;

    if (control.invalid && control.touched) {
      if (control.hasError('required')) {
        return 'سعر الخصم مطلوب';
      }

      if (control.hasError('min')) {
        return 'سعر الخصم يجب أن يكون 0.01 على الأقل';
      }
    }

    if (this.form.hasError('discountTooHigh') && control.touched) {
      return 'سعر الخصم يجب أن يكون أقل من السعر الأصلي';
    }

    return null;
  }

  startAtErrorMessage(): string | null {
    const control = this.form.controls.startAt;

    if (control.invalid && control.touched) {
      return 'تاريخ البداية مطلوب';
    }

    if (this.form.hasError('startAfterEnd') && control.touched) {
      return 'تاريخ البداية يجب أن يكون قبل تاريخ النهاية';
    }

    return null;
  }

  endAtErrorMessage(): string | null {
    const control = this.form.controls.endAt;

    if (control.invalid && control.touched) {
      return 'تاريخ النهاية مطلوب';
    }

    if (this.form.hasError('startAfterEnd') && control.touched) {
      return 'تاريخ البداية يجب أن يكون قبل تاريخ النهاية';
    }

    if (this.form.hasError('endNotFuture') && control.touched) {
      return 'تاريخ النهاية يجب أن يكون في المستقبل';
    }

    return null;
  }

  maximumQuantityPerCustomerErrorMessage(): string | null {
    const control = this.form.controls.maximumQuantityPerCustomer;
    return control.invalid && control.touched ? 'الحد الأدنى للكمية هو 1' : null;
  }

  submitForm(): void {
    if (this.form.invalid || this.formSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const editing = this.editingOffer();

    if (editing && !this.offerActions(editing).includes('edit')) {
      this.formModalOpen.set(false);
      return;
    }

    const raw = this.form.getRawValue();

    const body: OfferRequest = {
      productId: raw.productId,
      title: raw.title,
      description: raw.description.trim() ? raw.description : null,
      originalPrice: raw.originalPrice,
      discountedPrice: raw.discountedPrice,
      startAt: toIsoUtc(raw.startAt),
      endAt: toIsoUtc(raw.endAt),
      maximumQuantityPerCustomer: raw.maximumQuantityPerCustomer
    };

    this.formSaving.set(true);
    this.formErrorMessage.set(null);

    const request$ = editing ? this.offerService.update(editing.id, body) : this.offerService.create(body);

    request$.subscribe({
      next: () => {
        this.formSaving.set(false);
        this.formModalOpen.set(false);
        this.load();
      },
      error: (err: unknown) => {
        this.formSaving.set(false);
        this.formErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  submitForReview(offer: Offer): void {
    if (this.submittingId()) {
      return;
    }

    this.actionErrorMessage.set(null);
    this.submittingId.set(offer.id);

    this.offerService.submit(offer.id).subscribe({
      next: () => {
        this.submittingId.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.submittingId.set(null);
        this.actionErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  pauseOffer(offer: Offer): void {
    if (this.pausingId()) {
      return;
    }

    this.actionErrorMessage.set(null);
    this.pausingId.set(offer.id);

    this.offerService.pause(offer.id).subscribe({
      next: () => {
        this.pausingId.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.pausingId.set(null);
        this.actionErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  resumeOffer(offer: Offer): void {
    if (this.resumingId()) {
      return;
    }

    this.actionErrorMessage.set(null);
    this.resumingId.set(offer.id);

    this.offerService.resume(offer.id).subscribe({
      next: () => {
        this.resumingId.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.resumingId.set(null);
        this.actionErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openDeleteConfirm(offer: Offer): void {
    this.offerToDelete.set(offer);
    this.deleteErrorMessage.set(null);
    this.confirmDeleteOpen.set(true);
  }

  closeDeleteConfirm(): void {
    if (this.deleteSaving()) {
      return;
    }

    this.confirmDeleteOpen.set(false);
    this.offerToDelete.set(null);
  }

  confirmDelete(): void {
    const offer = this.offerToDelete();

    if (!offer || this.deleteSaving()) {
      return;
    }

    this.deleteSaving.set(true);
    this.deleteErrorMessage.set(null);

    this.offerService.remove(offer.id).subscribe({
      next: () => {
        this.deleteSaving.set(false);
        this.confirmDeleteOpen.set(false);
        this.offerToDelete.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.deleteSaving.set(false);
        this.deleteErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openCancelConfirm(offer: Offer): void {
    this.offerToCancel.set(offer);
    this.cancelErrorMessage.set(null);
    this.confirmCancelOpen.set(true);
  }

  closeCancelConfirm(): void {
    if (this.cancelSaving()) {
      return;
    }

    this.confirmCancelOpen.set(false);
    this.offerToCancel.set(null);
  }

  confirmCancel(): void {
    const offer = this.offerToCancel();

    if (!offer || this.cancelSaving()) {
      return;
    }

    this.cancelSaving.set(true);
    this.cancelErrorMessage.set(null);

    this.offerService.cancel(offer.id).subscribe({
      next: () => {
        this.cancelSaving.set(false);
        this.confirmCancelOpen.set(false);
        this.offerToCancel.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.cancelSaving.set(false);
        this.cancelErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
