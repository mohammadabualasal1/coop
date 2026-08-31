import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { BranchRequest, MerchantBranch } from '../models/branch.models';

@Injectable({ providedIn: 'root' })
export class BranchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/merchant-branches`;

  getAll(): Observable<MerchantBranch[]> {
    return this.http.get<MerchantBranch[]>(this.baseUrl);
  }

  getById(id: string): Observable<MerchantBranch> {
    return this.http.get<MerchantBranch>(`${this.baseUrl}/${id}`);
  }

  create(body: BranchRequest): Observable<MerchantBranch> {
    return this.http.post<MerchantBranch>(this.baseUrl, body);
  }

  update(id: string, body: BranchRequest): Observable<MerchantBranch> {
    return this.http.put<MerchantBranch>(`${this.baseUrl}/${id}`, body);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  setMain(id: string): Observable<MerchantBranch> {
    return this.http.put<MerchantBranch>(`${this.baseUrl}/${id}/set-main`, null);
  }
}
