import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { UserRole } from '../../../core/enums';
import { AuthService } from '../../../core/services/auth.service';
import { extractErrorMessage } from '../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiCardComponent,
  UiFieldComponent,
  UiPasswordToggleComponent
} from '../../../shared/components';

const PHONE_PATTERN = /^07[789][0-9]{7}$/;

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirmControl = group.get('confirmPassword');

  if (!confirmControl) {
    return null;
  }

  const { mismatch, ...otherErrors } = confirmControl.errors ?? {};

  if (confirmControl.value !== password) {
    confirmControl.setErrors({ ...otherErrors, mismatch: true });
  } else if (Object.keys(otherErrors).length > 0) {
    confirmControl.setErrors(otherErrors);
  } else {
    confirmControl.setErrors(null);
  }

  return null;
}

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    UiCardComponent,
    UiFieldComponent,
    UiButtonComponent,
    UiAlertComponent,
    UiPasswordToggleComponent
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group(
    {
      fullName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: passwordsMatchValidator }
  );

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal(false);
  readonly showConfirmPassword = signal(false);

  togglePasswordVisibility(): void {
    this.showPassword.update((value) => !value);
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword.update((value) => !value);
  }

  fullNameErrorMessage(): string | null {
    const control = this.form.controls.fullName;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'الاسم مطلوب';
    }

    if (control.hasError('minlength')) {
      return 'الاسم قصير جداً';
    }

    return null;
  }

  emailErrorMessage(): string | null {
    const control = this.form.controls.email;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'البريد الإلكتروني مطلوب';
    }

    if (control.hasError('email')) {
      return 'صيغة البريد الإلكتروني غير صحيحة';
    }

    return null;
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

  passwordErrorMessage(): string | null {
    const control = this.form.controls.password;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'كلمة المرور مطلوبة';
    }

    if (control.hasError('minlength')) {
      return 'كلمة المرور يجب أن تكون 6 أحرف على الأقل';
    }

    return null;
  }

  confirmPasswordErrorMessage(): string | null {
    const control = this.form.controls.confirmPassword;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'تأكيد كلمة المرور مطلوب';
    }

    if (control.hasError('mismatch')) {
      return 'كلمتا المرور غير متطابقتين';
    }

    return null;
  }

  submit(): void {
    if (this.submitting()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.submitting.set(true);

    const { fullName, email, phoneNumber, password } = this.form.getRawValue();

    this.auth
      .register({ fullName, email, phoneNumber, password, role: UserRole.Customer })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.router.navigateByUrl('/shop');
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          this.errorMessage.set(extractErrorMessage(err));
        }
      });
  }
}
