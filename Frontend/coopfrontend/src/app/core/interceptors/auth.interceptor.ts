import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthResponse, RefreshRequest } from '../models/auth.models';
import { TokenStorageService } from '../services/token-storage.service';

const SKIP_PATHS = [
  '/auth/login',
  '/auth/register',
  '/auth/refresh',
  '/auth/forgot-password',
  '/auth/reset-password',
  '/auth/send-verification-code',
  '/auth/verify-code'
];

let isRefreshing = false;
const refreshSubject = new BehaviorSubject<string | null>(null);

function isSkipped(url: string): boolean {
  if (!url.startsWith(environment.apiBaseUrl)) {
    return true;
  }

  return SKIP_PATHS.some((path) => url.includes(path));
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const storage = inject(TokenStorageService);
  const router = inject(Router);
  const http = inject(HttpClient);

  if (isSkipped(req.url)) {
    return next(req);
  }

  const accessToken = storage.accessToken;
  const authReq = accessToken
    ? req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      const refreshToken = storage.refreshToken;
      if (!refreshToken) {
        storage.clear();
        router.navigateByUrl('/login');
        return throwError(() => error);
      }

      if (!isRefreshing) {
        isRefreshing = true;
        refreshSubject.next(null);

        const body: RefreshRequest = { refreshToken };

        return http.post<AuthResponse>(`${environment.apiBaseUrl}/auth/refresh`, body).pipe(
          switchMap((res) => {
            storage.save(res);
            refreshSubject.next(res.accessToken);
            isRefreshing = false;
            return next(req.clone({ setHeaders: { Authorization: `Bearer ${res.accessToken}` } }));
          }),
          catchError((refreshError: unknown) => {
            isRefreshing = false;
            storage.clear();
            router.navigateByUrl('/login');
            return throwError(() => refreshError);
          })
        );
      }

      return refreshSubject.pipe(
        filter((token): token is string => token !== null),
        take(1),
        switchMap((token) => next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })))
      );
    })
  );
};
