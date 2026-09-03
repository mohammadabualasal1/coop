import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { OfferStatus, OfferStatusLabels, OfferStatusTones } from '../../../../core/enums';
import { MerchantBranch } from '../../../../core/models/branch.models';
import { BranchOffer, Offer, OfferRequest } from '../../../../core/models/offer.models';
import { Product } from '../../../../core/models/product.models';
import { BranchService } from '../../../../core/services/branch.service';
import { OfferService } from '../../../../core/services/offer.service';
import { ProductService } from '../../../../core/services/product.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiConfirmModalComponent,
  UiEmptyStateComponent,
  UiFieldComponent,
  UiModalComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';
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
    CoopDatePipe,
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiFieldComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiModalComponent,
    UiConfirmModalComponent
  ],
  templateUrl: './offers.html',
  styleUrl: './offers.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OffersComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly offerService = inject(OfferService);
  private readonly productService = inject(ProductService);
  private readonly branchService = inject(BranchService);

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

  readonly stockModalOpen = signal(false);
  readonly stockModalOfferId = signal<string | null>(null);
  readonly stockModalOffer = signal<Offer | null>(null);
  readonly stockModalStatus = signal<PageStatus>('loading');
  readonly stockModalErrorMessage = signal<string | null>(null);
  readonly stockBranches = signal<BranchOffer[]>([]);
  readonly merchantBranches = signal<MerchantBranch[]>([]);
  readonly stockGuardWarningVisible = signal(false);

  readonly stockModalTitle = computed(() => {
    const offer = this.stockModalOffer();
    return offer ? `الفروع والمخزون — ${offer.title}` : 'الفروع والمخزون';
  });

  readonly showStockGuardWarning = computed(
    () => this.stockGuardWarningVisible() && this.stockBranches().length === 0
  );

  readonly availableBranchesToAdd = computed(() => {
    const attachedIds = new Set(this.stockBranches().map((branchOffer) => branchOffer.merchantBranchId));
    return this.merchantBranches().filter((branch) => !attachedIds.has(branch.id));
  });

  readonly editingBranchOfferId = signal<string | null>(null);
  readonly editTotalStock = signal(0);
  readonly editIsAvailable = signal(true);
  readonly editStockSaving = signal(false);
  readonly editStockErrorMessage = signal<string | null>(null);

  readonly addBranchSaving = signal(false);
  readonly addBranchErrorMessage = signal<string | null>(null);

  readonly confirmRemoveBranchOpen = signal(false);
  readonly branchToRemove = signal<BranchOffer | null>(null);
  readonly removeBranchSaving = signal(false);
  readonly removeBranchErrorMessage = signal<string | null>(null);

  readonly addBranchForm = this.fb.nonNullable.group({
    merchantBranchId: ['', [Validators.required]],
    totalStock: [1, [Validators.required, Validators.min(1)]]
  });

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

  discountPercentagePreview(): number | null {
    const original = this.form.controls.originalPrice.value;
    const discounted = this.form.controls.discountedPrice.value;

    if (!original || original <= 0 || discounted == null) {
      return null;
    }

    return Math.round(((original - discounted) / original) * 1000) / 10;
  }

  discountPercentageDisplay(offer: Offer): number {
    return Math.round(offer.discountPercentage * 10) / 10;
  }

  openStockManagement(offer: Offer, showGuardWarning = false): void {
    this.stockModalOfferId.set(offer.id);
    this.stockModalOffer.set(offer);
    this.stockGuardWarningVisible.set(showGuardWarning);
    this.editingBranchOfferId.set(null);
    this.resetAddBranchForm();
    this.stockModalOpen.set(true);
    this.loadStockModalData(offer.id);
  }

  private loadStockModalData(offerId: string): void {
    this.stockModalStatus.set('loading');
    this.stockModalErrorMessage.set(null);

    forkJoin({
      offer: this.offerService.getById(offerId),
      branches: this.branchService.getAll()
    }).subscribe({
      next: ({ offer, branches }) => {
        // stockModalOfferId is the source of truth for the id used in every
        // branch API call — it is set synchronously from the card offer and
        // must never be replaced by this async response.
        this.stockModalOffer.set(offer);
        this.stockBranches.set(offer.branches ?? []);
        this.merchantBranches.set(branches);
        this.stockModalStatus.set('loaded');
      },
      error: (err: unknown) => {
        this.stockModalErrorMessage.set(extractErrorMessage(err));
        this.stockModalStatus.set('error');
      }
    });
  }

  closeStockModal(): void {
    if (this.addBranchSaving() || this.removeBranchSaving() || this.editStockSaving()) {
      return;
    }

    this.stockModalOpen.set(false);
    this.stockModalOfferId.set(null);
    this.stockModalOffer.set(null);
    this.stockBranches.set([]);
    this.merchantBranches.set([]);
    this.stockGuardWarningVisible.set(false);
    this.editingBranchOfferId.set(null);
    this.load();
  }

  availableStockFor(branchOffer: BranchOffer): number {
    return branchOffer.totalStock - branchOffer.reservedStock - branchOffer.soldStock;
  }

  minTotalStockFor(branchOffer: BranchOffer): number {
    return branchOffer.reservedStock + branchOffer.soldStock;
  }

  startEditBranch(branchOffer: BranchOffer): void {
    this.editingBranchOfferId.set(branchOffer.id);
    this.editTotalStock.set(branchOffer.totalStock);
    this.editIsAvailable.set(branchOffer.isAvailable);
    this.editStockErrorMessage.set(null);
  }

  cancelEditBranch(): void {
    if (this.editStockSaving()) {
      return;
    }

    this.editingBranchOfferId.set(null);
  }

  onEditTotalStockInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.editTotalStock.set(Number(target.value));
  }

  onEditIsAvailableChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.editIsAvailable.set(target.checked);
  }

  saveBranchStock(branchOffer: BranchOffer): void {
    const offerId = this.stockModalOfferId();

    if (!offerId || this.editStockSaving()) {
      return;
    }

    if (this.editTotalStock() < this.minTotalStockFor(branchOffer)) {
      this.editStockErrorMessage.set('لا يمكن أن يقل المخزون عن الكمية المحجوزة والمباعة');
      return;
    }

    this.editStockSaving.set(true);
    this.editStockErrorMessage.set(null);

    this.offerService
      .updateBranchStock(offerId, branchOffer.id, {
        totalStock: this.editTotalStock(),
        isAvailable: this.editIsAvailable()
      })
      .subscribe({
        next: () => {
          this.editStockSaving.set(false);
          this.editingBranchOfferId.set(null);
          this.loadStockModalData(offerId);
        },
        error: (err: unknown) => {
          this.editStockSaving.set(false);
          this.editStockErrorMessage.set(extractErrorMessage(err));
        }
      });
  }

  private resetAddBranchForm(): void {
    this.addBranchForm.reset({ merchantBranchId: '', totalStock: 1 });
    this.addBranchErrorMessage.set(null);
  }

  addBranchMerchantBranchIdErrorMessage(): string | null {
    const control = this.addBranchForm.controls.merchantBranchId;
    return control.invalid && control.touched ? 'الفرع مطلوب' : null;
  }

  addBranchTotalStockErrorMessage(): string | null {
    const control = this.addBranchForm.controls.totalStock;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'المخزون مطلوب';
    }

    return 'المخزون يجب أن يكون 1 على الأقل';
  }

  submitAddBranch(): void {
    const offerId = this.stockModalOfferId();

    if (!offerId || this.addBranchForm.invalid || this.addBranchSaving()) {
      this.addBranchForm.markAllAsTouched();
      return;
    }

    const raw = this.addBranchForm.getRawValue();

    this.addBranchSaving.set(true);
    this.addBranchErrorMessage.set(null);

    this.offerService
      .addBranch(offerId, { merchantBranchId: raw.merchantBranchId, totalStock: raw.totalStock })
      .subscribe({
        next: () => {
          this.addBranchSaving.set(false);
          this.resetAddBranchForm();
          this.loadStockModalData(offerId);
        },
        error: (err: unknown) => {
          this.addBranchSaving.set(false);
          this.addBranchErrorMessage.set(extractErrorMessage(err));
        }
      });
  }

  openRemoveBranchConfirm(branchOffer: BranchOffer): void {
    this.branchToRemove.set(branchOffer);
    this.removeBranchErrorMessage.set(null);
    this.confirmRemoveBranchOpen.set(true);
  }

  closeRemoveBranchConfirm(): void {
    if (this.removeBranchSaving()) {
      return;
    }

    this.confirmRemoveBranchOpen.set(false);
    this.branchToRemove.set(null);
  }

  confirmRemoveBranch(): void {
    const offerId = this.stockModalOfferId();
    const branchOffer = this.branchToRemove();

    if (!offerId || !branchOffer || this.removeBranchSaving()) {
      return;
    }

    this.removeBranchSaving.set(true);
    this.removeBranchErrorMessage.set(null);

    this.offerService.removeBranch(offerId, branchOffer.id).subscribe({
      next: () => {
        this.removeBranchSaving.set(false);
        this.confirmRemoveBranchOpen.set(false);
        this.branchToRemove.set(null);
        this.loadStockModalData(offerId);
      },
      error: (err: unknown) => {
        this.removeBranchSaving.set(false);
        this.removeBranchErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

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

    this.offerService.getById(offer.id).subscribe({
      next: (detail) => {
        this.submittingId.set(null);

        if (!detail.branches || detail.branches.length === 0) {
          this.openStockManagement(offer, true);
          return;
        }

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
