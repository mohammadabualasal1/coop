import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { UserRole } from '../enums';
import { FavoriteOffer } from '../models/marketplace.models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class FavoriteService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly baseUrl = `${environment.apiBaseUrl}/favorites`;

  private readonly _favoriteIds = signal<Set<string>>(new Set());
  private loaded = false;

  constructor() {
    effect(() => {
      const isCustomer = this.auth.isAuthenticated() && this.auth.role() === UserRole.Customer;

      if (isCustomer && !this.loaded) {
        this.loaded = true;
        this.getAll().subscribe({
          next: (favorites) => this._favoriteIds.set(new Set(favorites.map((favorite) => favorite.offerId))),
          error: () => this._favoriteIds.set(new Set())
        });
      }

      if (!isCustomer) {
        this.loaded = false;
        this._favoriteIds.set(new Set());
      }
    });
  }

  isFavorite(offerId: string): boolean {
    return this._favoriteIds().has(offerId);
  }

  getAll(): Observable<FavoriteOffer[]> {
    return this.http.get<FavoriteOffer[]>(this.baseUrl);
  }

  add(offerId: string): Observable<FavoriteOffer> {
    return this.http
      .post<FavoriteOffer>(`${this.baseUrl}/${offerId}`, null)
      .pipe(tap(() => this._favoriteIds.update((ids) => new Set(ids).add(offerId))));
  }

  remove(offerId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${offerId}`).pipe(
      tap(() =>
        this._favoriteIds.update((ids) => {
          const next = new Set(ids);
          next.delete(offerId);
          return next;
        })
      )
    );
  }
}
