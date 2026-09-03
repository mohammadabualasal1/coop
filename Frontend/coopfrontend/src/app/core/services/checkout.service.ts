import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CheckoutCalculateRequest, CheckoutCalculation } from '../models/checkout.models';

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/checkout`;

  calculate(body: CheckoutCalculateRequest): Observable<CheckoutCalculation> {
    return this.http.post<CheckoutCalculation>(`${this.baseUrl}/calculate`, body);
  }
}
