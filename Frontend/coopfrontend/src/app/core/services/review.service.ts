import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PagedReviews } from '../models/review.models';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/reviews`;

  getForMerchant(merchantId: string, pageNumber: number, pageSize: number): Observable<PagedReviews> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);

    return this.http.get<PagedReviews>(`${this.baseUrl}/merchant/${merchantId}`, { params });
  }
}
