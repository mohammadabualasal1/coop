import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { UserRole } from '../enums';
import { Cart, CartValidation } from '../models/cart.models';
import { extractErrorMessage } from '../utils/http-error';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly baseUrl = `${environment.apiBaseUrl}/cart`;

  private readonly _cart = signal<Cart | null>(null);
  readonly cart = this._cart.asReadonly();

  private readonly _initialLoadErrorMessage = signal<string | null>(null);
  /** Set when the eager cart load in the constructor fails, so a null `cart` can be
   * told apart from a genuinely empty one. Cleared as soon as any load succeeds. */
  readonly initialLoadErrorMessage = this._initialLoadErrorMessage.asReadonly();

  readonly itemCount = computed(
    () => this._cart()?.items.reduce((n, i) => n + i.quantity, 0) ?? 0
  );

  constructor() {
    if (this.auth.isAuthenticated() && this.auth.role() === UserRole.Customer) {
      // Failure is already recorded onto initialLoadErrorMessage by get() itself.
      this.get().subscribe({ error: () => {} });
    }
  }

  get(): Observable<Cart> {
    return this.http.get<Cart>(this.baseUrl).pipe(
      tap({
        next: (cart) => {
          this._cart.set(cart);
          this._initialLoadErrorMessage.set(null);
        },
        error: (err: unknown) => this._initialLoadErrorMessage.set(extractErrorMessage(err))
      })
    );
  }

  addItem(offerId: string, quantity: number): Observable<Cart> {
    return this.http
      .post<Cart>(`${this.baseUrl}/items`, { offerId, quantity })
      .pipe(tap((cart) => this._cart.set(cart)));
  }

  updateItem(itemId: string, quantity: number): Observable<Cart> {
    return this.http
      .put<Cart>(`${this.baseUrl}/items/${itemId}`, { quantity })
      .pipe(tap((cart) => this._cart.set(cart)));
  }

  removeItem(itemId: string): Observable<Cart> {
    return this.http
      .delete<Cart>(`${this.baseUrl}/items/${itemId}`)
      .pipe(tap((cart) => this._cart.set(cart)));
  }

  clear(): Observable<Cart> {
    return this.http.delete<Cart>(this.baseUrl).pipe(tap((cart) => this._cart.set(cart)));
  }

  validate(): Observable<CartValidation> {
    return this.http
      .get<CartValidation>(`${this.baseUrl}/validate`)
      .pipe(tap((result) => this._cart.set(result.cart)));
  }

  /** Resets the local cart signal without an HTTP call — for when the backend has
   * already emptied the cart as a side effect (e.g. placing an order). */
  reset(): void {
    this._cart.set(null);
  }
}
