import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  AuthUser,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  LoginRequest,
  LogoutRequest,
  RefreshRequest,
  RegisterRequest,
  ResetPasswordRequest,
  SendVerificationCodeRequest,
  SimulatedCodeResponse,
  UpdateProfileRequest,
  VerifyCodeRequest
} from '../models/auth.models';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(TokenStorageService);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  private readonly _user = signal<AuthUser | null>(null);
  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly role = computed(() => this._user()?.role ?? null);

  constructor() {
    this._user.set(this.storage.user);
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/register`, request)
      .pipe(tap((res) => this.persistSession(res)));
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/login`, request)
      .pipe(tap((res) => this.persistSession(res)));
  }

  refresh(request: RefreshRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/refresh`, request)
      .pipe(tap((res) => this.persistSession(res)));
  }

  logout(request: LogoutRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, request).pipe(
      tap({
        next: () => this.clearSession(),
        error: () => this.clearSession()
      })
    );
  }

  me(): Observable<AuthUser> {
    return this.http
      .get<AuthUser>(`${this.baseUrl}/me`)
      .pipe(tap((user) => this._user.set(user)));
  }

  updateProfile(request: UpdateProfileRequest): Observable<AuthUser> {
    return this.http.put<AuthUser>(`${this.baseUrl}/me`, request);
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/change-password`, request);
  }

  sendVerificationCode(request: SendVerificationCodeRequest): Observable<SimulatedCodeResponse> {
    return this.http.post<SimulatedCodeResponse>(`${this.baseUrl}/send-verification-code`, request);
  }

  verifyCode(request: VerifyCodeRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/verify-code`, request);
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<SimulatedCodeResponse> {
    return this.http.post<SimulatedCodeResponse>(`${this.baseUrl}/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/reset-password`, request);
  }

  private persistSession(res: AuthResponse): void {
    this.storage.save(res);
    this._user.set(res.user);
  }

  private clearSession(): void {
    this.storage.clear();
    this._user.set(null);
  }
}
