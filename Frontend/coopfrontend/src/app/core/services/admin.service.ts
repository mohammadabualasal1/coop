import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PendingOffer } from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/admin`;

  getPendingOffers(): Observable<PendingOffer[]> {
    return this.http.get<PendingOffer[]>(`${this.baseUrl}/offers/pending`);
  }

  approveOffer(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/offers/${id}/approve`, null);
  }

  rejectOffer(id: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/offers/${id}/reject`, { reason });
  }
}
