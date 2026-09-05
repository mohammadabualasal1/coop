import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';

import { VerificationStatusLabels, VerificationStatusTones } from '../../../../core/enums';
import { Merchant, UpdateMerchantRequest } from '../../../../core/models/merchant.models';
import { MerchantService } from '../../../../core/services/merchant.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiFieldComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';

const PHONE_PATTERN = /^07[789][0-9]{7}$/;
const SAVE_SUCCESS_DISPLAY_MS = 4000;

type PageStatus = 'loading' | 'error' | 'loaded';

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
  selector: 'app-merchant-profile',
  imports: [
    ReactiveFormsModule,
    CoopDatePipe,
    UiCardComponent,
    UiFieldComponent,
    UiButtonComponent,
    UiAlertComponent,
    UiBadgeComponent,
    UiSpinnerComponent
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfileComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly merchantService = inject(MerchantService);

  readonly VerificationStatusLabels = VerificationStatusLabels;
  readonly VerificationStatusTones = VerificationStatusTones;

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly merchant = signal<Merchant | null>(null);

  readonly saving = signal(false);
  readonly saveErrorMessage = signal<string | null>(null);
  readonly saveSuccess = signal(false);

  readonly logoPreviewFailed = signal(false);
  readonly coverPreviewFailed = signal(false);

  private successTimeoutId: ReturnType<typeof setTimeout> | undefined;

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
    contactEmail: ['', [Validators.required, Validators.email]],
    contactPhone: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    logoUrl: ['', [optionalUrlValidator]],
    coverImageUrl: ['', [optionalUrlValidator]]
  });

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    clearTimeout(this.successTimeoutId);
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.merchantService.getMy().subscribe({
      next: (merchant) => {
        this.merchant.set(merchant);
        this.patchForm(merchant);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  nameErrorMessage(): string | null {
    const control = this.form.controls.name;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'اسم المتجر مطلوب';
    }

    if (control.hasError('minlength')) {
      return 'اسم المتجر قصير جداً';
    }

    return null;
  }

  contactEmailErrorMessage(): string | null {
    const control = this.form.controls.contactEmail;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'البريد الإلكتروني للتواصل مطلوب';
    }

    if (control.hasError('email')) {
      return 'صيغة البريد الإلكتروني غير صحيحة';
    }

    return null;
  }

  contactPhoneErrorMessage(): string | null {
    const control = this.form.controls.contactPhone;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'رقم هاتف التواصل مطلوب';
    }

    if (control.hasError('pattern')) {
      return 'رقم الهاتف يجب أن يبدأ بـ 077 أو 078 أو 079 ويتكون من 10 أرقام';
    }

    return null;
  }

  logoUrlErrorMessage(): string | null {
    const control = this.form.controls.logoUrl;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('url')) {
      return 'الرابط غير صالح';
    }

    return null;
  }

  coverImageUrlErrorMessage(): string | null {
    const control = this.form.controls.coverImageUrl;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('url')) {
      return 'الرابط غير صالح';
    }

    return null;
  }

  logoUrlPreview(): string | null {
    const value = (this.form.controls.logoUrl.value ?? '').trim();
    return isValidUrl(value) ? value : null;
  }

  coverImageUrlPreview(): string | null {
    const value = (this.form.controls.coverImageUrl.value ?? '').trim();
    return isValidUrl(value) ? value : null;
  }

  averageRatingLabel(): string {
    const merchant = this.merchant();

    if (!merchant || merchant.averageRating === null) {
      return 'لا توجد تقييمات بعد';
    }

    return `${merchant.averageRating.toFixed(1)} / 5`;
  }

  submit(): void {
    if (this.form.pristine || this.form.invalid || this.saving()) {
      return;
    }

    const raw = this.form.getRawValue();

    const body: UpdateMerchantRequest = {
      name: raw.name,
      description: raw.description.trim() ? raw.description : null,
      contactEmail: raw.contactEmail,
      contactPhone: raw.contactPhone,
      logoUrl: raw.logoUrl.trim() ? raw.logoUrl : null,
      coverImageUrl: raw.coverImageUrl.trim() ? raw.coverImageUrl : null
    };

    this.saving.set(true);
    this.saveErrorMessage.set(null);

    this.merchantService.updateMy(body).subscribe({
      next: (merchant) => {
        this.saving.set(false);
        this.merchant.set(merchant);
        this.patchForm(merchant);
        this.showSaveSuccess();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.saveErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  private patchForm(merchant: Merchant): void {
    this.form.reset({
      name: merchant.name,
      description: merchant.description ?? '',
      contactEmail: merchant.contactEmail,
      contactPhone: merchant.contactPhone,
      logoUrl: merchant.logoUrl ?? '',
      coverImageUrl: merchant.coverImageUrl ?? ''
    });
  }

  private showSaveSuccess(): void {
    this.saveSuccess.set(true);

    if (this.successTimeoutId) {
      clearTimeout(this.successTimeoutId);
    }

    this.successTimeoutId = setTimeout(() => this.saveSuccess.set(false), SAVE_SUCCESS_DISPLAY_MS);
  }
}
