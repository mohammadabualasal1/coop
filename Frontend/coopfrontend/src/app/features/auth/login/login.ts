import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { UserRole } from '../../../core/enums';
import { AuthService } from '../../../core/services/auth.service';
import { TokenStorageService } from '../../../core/services/token-storage.service';
import { extractErrorMessage } from '../../../core/utils/http-error';

const DRIVER_ONLY_MESSAGE = 'حساب السائق يعمل من تطبيق الجوال فقط.';

const ROLE_HOME_URL: Partial<Record<UserRole, string>> = {
  [UserRole.Customer]: '/shop',
  [UserRole.Merchant]: '/merchant',
  [UserRole.Admin]: '/admin'
};

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
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
    const defaultUrl = ROLE_HOME_URL[role];

    if (!defaultUrl) {
      this.rejectDriverAccount();
      return;
    }

    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    const targetUrl = returnUrl && returnUrl.startsWith(defaultUrl) ? returnUrl : defaultUrl;

    this.submitting.set(false);
    this.router.navigateByUrl(targetUrl);
  }

  private rejectDriverAccount(): void {
    const refreshToken = this.tokenStorage.refreshToken ?? '';

    this.auth.logout({ refreshToken }).subscribe({
      next: () => this.finishWithDriverMessage(),
      error: () => this.finishWithDriverMessage()
    });
  }

  private finishWithDriverMessage(): void {
    this.submitting.set(false);
    this.errorMessage.set(DRIVER_ONLY_MESSAGE);
  }
}
