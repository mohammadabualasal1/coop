import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { UserRole } from '../../../core/enums';
import { AuthService } from '../../../core/services/auth.service';
import { TokenStorageService } from '../../../core/services/token-storage.service';
import { extractErrorMessage } from '../../../core/utils/http-error';

const MOBILE_ONLY_MESSAGE =
  'هذا الحساب مخصص لتطبيق الجوال. لوحة التحكم للتجار والمشرفين فقط.';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

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

    const { email, password } = this.form.getRawValue();

    this.auth.login({ email, password }).subscribe({
      next: (res) => this.handleSuccess(res.user.role),
      error: (err: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(extractErrorMessage(err));
      }
    });
  }

  private handleSuccess(role: UserRole): void {
    if (role === UserRole.Merchant || role === UserRole.Admin) {
      const defaultUrl = role === UserRole.Merchant ? '/merchant' : '/admin';
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
      const targetUrl = returnUrl && returnUrl.startsWith(defaultUrl) ? returnUrl : defaultUrl;

      this.submitting.set(false);
      this.router.navigateByUrl(targetUrl);
      return;
    }

    this.rejectMobileOnlyAccount();
  }

  private rejectMobileOnlyAccount(): void {
    const refreshToken = this.tokenStorage.refreshToken ?? '';

    this.auth.logout({ refreshToken }).subscribe({
      next: () => this.finishWithMobileOnlyMessage(),
      error: () => this.finishWithMobileOnlyMessage()
    });
  }

  private finishWithMobileOnlyMessage(): void {
    this.submitting.set(false);
    this.errorMessage.set(MOBILE_ONLY_MESSAGE);
  }
}
