import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { BranchRequest, MerchantBranch } from '../../../../core/models/branch.models';
import { BranchService } from '../../../../core/services/branch.service';
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

const PHONE_PATTERN = /^07[789][0-9]{7}$/;

type PageStatus = 'loading' | 'error' | 'loaded';

function appendSeconds(time: string): string {
  return `${time}:00`;
}

function stripSeconds(time: string): string {
  return time.slice(0, 5);
}

@Component({
  selector: 'app-merchant-branches',
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
  templateUrl: './branches.html',
  styleUrl: './branches.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BranchesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly branchService = inject(BranchService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly branches = signal<MerchantBranch[]>([]);

  readonly actionErrorMessage = signal<string | null>(null);
  readonly settingMainId = signal<string | null>(null);

  readonly formModalOpen = signal(false);
  readonly editingBranch = signal<MerchantBranch | null>(null);
  readonly formSaving = signal(false);
  readonly formErrorMessage = signal<string | null>(null);
  readonly modalTitle = computed(() => (this.editingBranch() ? 'تعديل الفرع' : 'إضافة فرع'));

  readonly confirmDeactivateOpen = signal(false);
  readonly branchToDeactivate = signal<MerchantBranch | null>(null);
  readonly deactivateSaving = signal(false);
  readonly deactivateErrorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    city: ['', [Validators.required]],
    area: ['', [Validators.required]],
    address: ['', [Validators.required]],
    phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    latitude: [0, [Validators.required, Validators.min(-90), Validators.max(90)]],
    longitude: [0, [Validators.required, Validators.min(-180), Validators.max(180)]],
    openingTime: ['', [Validators.required]],
    closingTime: ['', [Validators.required]],
    deliveryRadiusKm: [1, [Validators.required, Validators.min(0.5)]],
    minimumOrderAmount: [0, [Validators.required, Validators.min(0)]],
    baseDeliveryFee: [0, [Validators.required, Validators.min(0)]]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.branchService.getAll().subscribe({
      next: (branches) => {
        this.branches.set(branches);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  addressLine(branch: MerchantBranch): string {
    return `${branch.city} — ${branch.area} — ${branch.address}`;
  }

  formattedHours(branch: MerchantBranch): string {
    return `${stripSeconds(branch.openingTime)} - ${stripSeconds(branch.closingTime)}`;
  }

  openCreateModal(): void {
    this.editingBranch.set(null);
    this.formErrorMessage.set(null);
    this.form.reset({
      name: '',
      city: '',
      area: '',
      address: '',
      phoneNumber: '',
      latitude: 0,
      longitude: 0,
      openingTime: '',
      closingTime: '',
      deliveryRadiusKm: 1,
      minimumOrderAmount: 0,
      baseDeliveryFee: 0
    });
    this.formModalOpen.set(true);
  }

  openEditModal(branch: MerchantBranch): void {
    this.editingBranch.set(branch);
    this.formErrorMessage.set(null);
    this.form.reset({
      name: branch.name,
      city: branch.city,
      area: branch.area,
      address: branch.address,
      phoneNumber: branch.phoneNumber,
      latitude: branch.latitude,
      longitude: branch.longitude,
      openingTime: stripSeconds(branch.openingTime),
      closingTime: stripSeconds(branch.closingTime),
      deliveryRadiusKm: branch.deliveryRadiusKm,
      minimumOrderAmount: branch.minimumOrderAmount,
      baseDeliveryFee: branch.baseDeliveryFee
    });
    this.formModalOpen.set(true);
  }

  closeFormModal(): void {
    if (this.formSaving()) {
      return;
    }

    this.formModalOpen.set(false);
  }

  nameErrorMessage(): string | null {
    const control = this.form.controls.name;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'اسم الفرع مطلوب';
    }

    if (control.hasError('minlength')) {
      return 'اسم الفرع قصير جداً';
    }

    return null;
  }

  cityErrorMessage(): string | null {
    return this.form.controls.city.invalid ? 'المدينة مطلوبة' : null;
  }

  areaErrorMessage(): string | null {
    return this.form.controls.area.invalid ? 'المنطقة مطلوبة' : null;
  }

  addressErrorMessage(): string | null {
    return this.form.controls.address.invalid ? 'العنوان مطلوب' : null;
  }

  phoneNumberErrorMessage(): string | null {
    const control = this.form.controls.phoneNumber;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'رقم الهاتف مطلوب';
    }

    if (control.hasError('pattern')) {
      return 'رقم الهاتف يجب أن يبدأ بـ 077 أو 078 أو 079 ويتكون من 10 أرقام';
    }

    return null;
  }

  latitudeErrorMessage(): string | null {
    const control = this.form.controls.latitude;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'خط العرض مطلوب';
    }

    return 'خط العرض يجب أن يكون بين -90 و 90';
  }

  longitudeErrorMessage(): string | null {
    const control = this.form.controls.longitude;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'خط الطول مطلوب';
    }

    return 'خط الطول يجب أن يكون بين -180 و 180';
  }

  openingTimeErrorMessage(): string | null {
    return this.form.controls.openingTime.invalid ? 'وقت الفتح مطلوب' : null;
  }

  closingTimeErrorMessage(): string | null {
    return this.form.controls.closingTime.invalid ? 'وقت الإغلاق مطلوب' : null;
  }

  deliveryRadiusKmErrorMessage(): string | null {
    const control = this.form.controls.deliveryRadiusKm;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'نطاق التوصيل مطلوب';
    }

    return 'نطاق التوصيل يجب أن يكون 0.5 كم على الأقل';
  }

  minimumOrderAmountErrorMessage(): string | null {
    const control = this.form.controls.minimumOrderAmount;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'الحد الأدنى للطلب مطلوب';
    }

    return 'القيمة يجب أن تكون 0 أو أكثر';
  }

  baseDeliveryFeeErrorMessage(): string | null {
    const control = this.form.controls.baseDeliveryFee;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'رسوم التوصيل مطلوبة';
    }

    return 'القيمة يجب أن تكون 0 أو أكثر';
  }

  submitForm(): void {
    if (this.form.invalid || this.formSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();

    const body: BranchRequest = {
      name: raw.name,
      city: raw.city,
      area: raw.area,
      address: raw.address,
      phoneNumber: raw.phoneNumber,
      latitude: raw.latitude,
      longitude: raw.longitude,
      openingTime: appendSeconds(raw.openingTime),
      closingTime: appendSeconds(raw.closingTime),
      deliveryRadiusKm: raw.deliveryRadiusKm,
      minimumOrderAmount: raw.minimumOrderAmount,
      baseDeliveryFee: raw.baseDeliveryFee
    };

    this.formSaving.set(true);
    this.formErrorMessage.set(null);

    const editing = this.editingBranch();
    const request$ = editing
      ? this.branchService.update(editing.id, body)
      : this.branchService.create(body);

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

  setMain(branch: MerchantBranch): void {
    this.actionErrorMessage.set(null);
    this.settingMainId.set(branch.id);

    this.branchService.setMain(branch.id).subscribe({
      next: () => {
        this.settingMainId.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.settingMainId.set(null);
        this.actionErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openDeactivateConfirm(branch: MerchantBranch): void {
    this.branchToDeactivate.set(branch);
    this.deactivateErrorMessage.set(null);
    this.confirmDeactivateOpen.set(true);
  }

  closeDeactivateConfirm(): void {
    if (this.deactivateSaving()) {
      return;
    }

    this.confirmDeactivateOpen.set(false);
    this.branchToDeactivate.set(null);
  }

  confirmDeactivate(): void {
    const branch = this.branchToDeactivate();

    if (!branch || this.deactivateSaving()) {
      return;
    }

    this.deactivateSaving.set(true);
    this.deactivateErrorMessage.set(null);

    this.branchService.deactivate(branch.id).subscribe({
      next: () => {
        this.deactivateSaving.set(false);
        this.confirmDeactivateOpen.set(false);
        this.branchToDeactivate.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.deactivateSaving.set(false);
        this.deactivateErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
