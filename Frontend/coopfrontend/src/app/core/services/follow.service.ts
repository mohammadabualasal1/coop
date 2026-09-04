import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { UserRole } from '../enums';
import { MerchantSummary } from '../models/marketplace.models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class FollowService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly baseUrl = `${environment.apiBaseUrl}/follows`;

  private readonly _followedIds = signal<Set<string>>(new Set());
  private loaded = false;

  constructor() {
    effect(() => {
      const isCustomer = this.auth.isAuthenticated() && this.auth.role() === UserRole.Customer;

      if (isCustomer && !this.loaded) {
        this.loaded = true;
        this.getAll().subscribe({
          next: (merchants) => this._followedIds.set(new Set(merchants.map((merchant) => merchant.id))),
          error: () => this._followedIds.set(new Set())
        });
      }

      if (!isCustomer) {
        this.loaded = false;
        this._followedIds.set(new Set());
      }
    });
  }

  isFollowing(merchantId: string): boolean {
    return this._followedIds().has(merchantId);
  }

  getAll(): Observable<MerchantSummary[]> {
    return this.http.get<MerchantSummary[]>(this.baseUrl);
  }

  follow(merchantId: string): Observable<void> {
    return this.http
      .post<void>(`${this.baseUrl}/${merchantId}`, null)
      .pipe(tap(() => this._followedIds.update((ids) => new Set(ids).add(merchantId))));
  }

  unfollow(merchantId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${merchantId}`).pipe(
      tap(() =>
        this._followedIds.update((ids) => {
          const next = new Set(ids);
          next.delete(merchantId);
          return next;
        })
      )
    );
  }
}
