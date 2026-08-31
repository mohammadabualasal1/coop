import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Merchant, UpdateMerchantRequest } from '../models/merchant.models';

@Injectable({ providedIn: 'root' })
export class MerchantService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/merchants`;

  private readonly _merchant = signal<Merchant | null>(null);
  readonly merchant = this._merchant.asReadonly();

  getMy(): Observable<Merchant> {
    return this.http
      .get<Merchant>(`${this.baseUrl}/my`)
      .pipe(tap((merchant) => this._merchant.set(merchant)));
  }

  updateMy(body: UpdateMerchantRequest): Observable<Merchant> {
    return this.http
      .put<Merchant>(`${this.baseUrl}/my`, body)
      .pipe(tap((merchant) => this._merchant.set(merchant)));
  }
}
