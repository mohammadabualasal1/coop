import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CustomerOrderDetail,
  CustomerOrderSummary,
  OrderTracking
} from '../models/customer-order.models';
import { PlaceOrderRequest, PlaceOrderResponse } from '../models/order.models';

export interface DeliveryCodeResponse {
  code: string;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/orders`;

  place(body: PlaceOrderRequest): Observable<PlaceOrderResponse> {
    return this.http.post<PlaceOrderResponse>(this.baseUrl, body);
  }

  getAll(): Observable<CustomerOrderSummary[]> {
    return this.http.get<CustomerOrderSummary[]>(this.baseUrl);
  }

  getById(id: string): Observable<CustomerOrderDetail> {
    return this.http.get<CustomerOrderDetail>(`${this.baseUrl}/${id}`);
  }

  getTracking(id: string): Observable<OrderTracking> {
    return this.http.get<OrderTracking>(`${this.baseUrl}/${id}/tracking`);
  }

  cancel(id: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, { reason });
  }

  deliveryCode(id: string): Observable<DeliveryCodeResponse> {
    return this.http.post<DeliveryCodeResponse>(`${this.baseUrl}/${id}/delivery-code`, null);
  }

  confirmDelivery(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/confirm-delivery`, null);
  }
}
