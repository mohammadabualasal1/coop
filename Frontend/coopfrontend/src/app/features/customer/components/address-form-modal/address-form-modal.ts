import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { Address, AddressRequest } from '../../../../core/models/address.models';
import { AddressService } from '../../../../core/services/address.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiFieldComponent,
  UiModalComponent
} from '../../../../shared/components';

const PHONE_PATTERN = /^07[789][0-9]{7}$/;
const LABEL_PRESETS = ['المنزل', 'العمل', 'أخرى'];

@Component({
  selector: 'app-address-form-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiFieldComponent, UiAlertComponent, UiButtonComponent],
  templateUrl: './address-form-modal.html',
  styleUrl: './address-form-modal.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddressFormModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly addressService = inject(AddressService);

  readonly open = input(false);
  readonly editingAddress = input<Address | null>(null);

  readonly closed = output<void>();
  readonly saved = output<Address>();

  readonly labelPresets = LABEL_PRESETS;

  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    label: ['', [Validators.required]],
    contactName: ['', [Validators.required, Validators.minLength(2)]],
    contactPhone: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    city: ['', [Validators.required]],
    area: ['', [Validators.required]],
    street: ['', [Validators.required]],
    building: [''],
    floor: [''],
    additionalDirections: [''],
    latitude: [0, [Validators.required, Validators.min(-90), Validators.max(90)]],
    longitude: [0, [Validators.required, Validators.min(-180), Validators.max(180)]]
  });

  constructor() {
    effect(() => {
      if (!this.open()) {
        return;
      }

      const editing = this.editingAddress();
      this.errorMessage.set(null);
      this.form.reset({
        label: editing?.label ?? '',
        contactName: editing?.contactName ?? '',
        contactPhone: editing?.contactPhone ?? '',
        city: editing?.city ?? '',
        area: editing?.area ?? '',
        street: editing?.street ?? '',
        building: editing?.building ?? '',
        floor: editing?.floor ?? '',
        additionalDirections: editing?.additionalDirections ?? '',
        latitude: editing?.latitude ?? 0,
        longitude: editing?.longitude ?? 0
      });
    });
  }

  pickLabel(value: string): void {
    this.form.controls.label.setValue(value);
  }

  labelErrorMessage(): string | null {
    return this.form.controls.label.invalid && this.form.controls.label.touched
      ? 'التسمية مطلوبة'
      : null;
  }

  contactNameErrorMessage(): string | null {
    const control = this.form.controls.contactName;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'اسم المستلم مطلوب';
    }

    return 'اسم المستلم قصير جداً';
  }

  contactPhoneErrorMessage(): string | null {
    const control = this.form.controls.contactPhone;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'رقم الهاتف مطلوب';
    }

    return 'رقم الهاتف يجب أن يبدأ بـ 077 أو 078 أو 079 ويتكون من 10 أرقام';
  }

  cityErrorMessage(): string | null {
    return this.form.controls.city.invalid && this.form.controls.city.touched ? 'المدينة مطلوبة' : null;
  }

  areaErrorMessage(): string | null {
    return this.form.controls.area.invalid && this.form.controls.area.touched ? 'المنطقة مطلوبة' : null;
  }

  streetErrorMessage(): string | null {
    return this.form.controls.street.invalid && this.form.controls.street.touched
      ? 'الشارع مطلوب'
      : null;
  }

  latitudeErrorMessage(): string | null {
    const control = this.form.controls.latitude;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'خط العرض مطلوب';
    }

    return 'خط العرض يجب أن يكون بين -90 و 90';
  }

  longitudeErrorMessage(): string | null {
    const control = this.form.controls.longitude;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'خط الطول مطلوب';
    }

    return 'خط الطول يجب أن يكون بين -180 و 180';
  }

  cancel(): void {
    if (this.saving()) {
      return;
    }

    this.closed.emit();
  }

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();

    const body: AddressRequest = {
      label: raw.label,
      contactName: raw.contactName,
      contactPhone: raw.contactPhone,
      city: raw.city,
      area: raw.area,
      street: raw.street,
      building: raw.building.trim() ? raw.building : null,
      floor: raw.floor.trim() ? raw.floor : null,
      additionalDirections: raw.additionalDirections.trim() ? raw.additionalDirections : null,
      latitude: raw.latitude,
      longitude: raw.longitude
    };

    this.saving.set(true);
    this.errorMessage.set(null);

    const editing = this.editingAddress();
    const request$ = editing
      ? this.addressService.update(editing.id, body)
      : this.addressService.create(body);

    request$.subscribe({
      next: (address) => {
        this.saving.set(false);
        this.saved.emit(address);
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.errorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
