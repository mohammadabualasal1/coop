import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AdminUser,
  CreateDriverUserRequest,
  CreateMerchantUserRequest,
  GetUsersParams,
  PagedResult,
  PendingOffer
} from '../models/admin.models';

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

  getUsers(params: GetUsersParams): Observable<PagedResult<AdminUser>> {
    let httpParams = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);

    if (params.role != null) {
      httpParams = httpParams.set('role', params.role);
    }

    if (params.status != null) {
      httpParams = httpParams.set('status', params.status);
    }

    if (params.search) {
      httpParams = httpParams.set('search', params.search);
    }

    return this.http.get<PagedResult<AdminUser>>(`${this.baseUrl}/users`, { params: httpParams });
  }

  createMerchantUser(body: CreateMerchantUserRequest): Observable<AdminUser> {
    return this.http.post<AdminUser>(`${this.baseUrl}/users/merchant`, body);
  }

  createDriverUser(body: CreateDriverUserRequest): Observable<AdminUser> {
    return this.http.post<AdminUser>(`${this.baseUrl}/users/driver`, body);
  }

  suspendUser(id: string, reason: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/users/${id}/suspend`, { reason });
  }

  activateUser(id: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/users/${id}/activate`, null);
  }
}
