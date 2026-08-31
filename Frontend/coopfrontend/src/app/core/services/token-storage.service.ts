import { Injectable } from '@angular/core';

import { AuthResponse, AuthUser } from '../models/auth.models';

const ACCESS_KEY = 'coop_access';
const REFRESH_KEY = 'coop_refresh';
const USER_KEY = 'coop_user';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  save(auth: AuthResponse): void {
    try {
      localStorage.setItem(ACCESS_KEY, auth.accessToken);
      localStorage.setItem(REFRESH_KEY, auth.refreshToken);
      localStorage.setItem(USER_KEY, JSON.stringify(auth.user));
    } catch {
      // localStorage may be unavailable (private browsing, disabled storage, etc.)
    }
  }

  clear(): void {
    try {
      localStorage.removeItem(ACCESS_KEY);
      localStorage.removeItem(REFRESH_KEY);
      localStorage.removeItem(USER_KEY);
    } catch {
      // localStorage may be unavailable (private browsing, disabled storage, etc.)
    }
  }

  get accessToken(): string | null {
    try {
      return localStorage.getItem(ACCESS_KEY);
    } catch {
      return null;
    }
  }

  get refreshToken(): string | null {
    try {
      return localStorage.getItem(REFRESH_KEY);
    } catch {
      return null;
    }
  }

  get user(): AuthUser | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as AuthUser) : null;
    } catch {
      return null;
    }
  }
}
