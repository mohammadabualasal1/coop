import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { OrderStatus } from '../enums';
import {
  MarkReadyResponse,
  MerchantOrderDetail,
  MerchantOrderSummary,
  PickupCodeResponse
} from '../models/order.models';

@Injectable({ providedIn: 'root' })
export class MerchantOrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/merchant-orders`;

  getAll(status?: OrderStatus | null): Observable<MerchantOrderSummary[]> {
    let params = new HttpParams();

    if (status != null) {
      params = params.set('status', status);
    }

    return this.http.get<MerchantOrderSummary[]>(this.baseUrl, { params });
  }

  getById(id: string): Observable<MerchantOrderDetail> {
    return this.http.get<MerchantOrderDetail>(`${this.baseUrl}/${id}`);
  }

  accept(id: string): Observable<MerchantOrderSummary> {
    return this.http.post<MerchantOrderSummary>(`${this.baseUrl}/${id}/accept`, null);
  }

  reject(id: string, reason: string): Observable<MerchantOrderSummary> {
    return this.http.post<MerchantOrderSummary>(`${this.baseUrl}/${id}/reject`, { reason });
  }

  markReady(id: string): Observable<MarkReadyResponse> {
    return this.http.post<MarkReadyResponse>(`${this.baseUrl}/${id}/ready`, null);
  }

  reissuePickupCode(id: string): Observable<PickupCodeResponse> {
    return this.http.post<PickupCodeResponse>(`${this.baseUrl}/${id}/pickup-code`, null);
  }
}
