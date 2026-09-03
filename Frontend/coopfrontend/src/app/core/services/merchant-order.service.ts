import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { OrderStatus } from '../enums';
import { MarkReadyResponse, MerchantOrder, PickupCodeResponse } from '../models/order.models';

@Injectable({ providedIn: 'root' })
export class MerchantOrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/merchant-orders`;

  getAll(status?: OrderStatus | null): Observable<MerchantOrder[]> {
    let params = new HttpParams();

    if (status != null) {
      params = params.set('status', status);
    }

    return this.http.get<MerchantOrder[]>(this.baseUrl, { params });
  }

  getById(id: string): Observable<MerchantOrder> {
    return this.http.get<MerchantOrder>(`${this.baseUrl}/${id}`);
  }

  accept(id: string): Observable<MerchantOrder> {
    return this.http.post<MerchantOrder>(`${this.baseUrl}/${id}/accept`, null);
  }

  reject(id: string, reason: string): Observable<MerchantOrder> {
    return this.http.post<MerchantOrder>(`${this.baseUrl}/${id}/reject`, { reason });
  }

  markReady(id: string): Observable<MarkReadyResponse> {
    return this.http.post<MarkReadyResponse>(`${this.baseUrl}/${id}/ready`, null);
  }

  reissuePickupCode(id: string): Observable<PickupCodeResponse> {
    return this.http.post<PickupCodeResponse>(`${this.baseUrl}/${id}/pickup-code`, null);
  }
}
