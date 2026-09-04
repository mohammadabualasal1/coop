import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { UserRole } from '../enums';
import { FollowedMerchant } from '../models/marketplace.models';
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
          next: (follows) => this._followedIds.set(new Set(follows.map((follow) => follow.merchantId))),
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

  getAll(): Observable<FollowedMerchant[]> {
    return this.http.get<FollowedMerchant[]>(this.baseUrl);
  }

  follow(merchantId: string): Observable<FollowedMerchant> {
    return this.http
      .post<FollowedMerchant>(`${this.baseUrl}/${merchantId}`, null)
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
