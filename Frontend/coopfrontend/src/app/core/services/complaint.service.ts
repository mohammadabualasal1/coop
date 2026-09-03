import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ComplaintStatus } from '../enums';
import { Complaint, CreateComplaintRequest } from '../models/complaint.models';

@Injectable({ providedIn: 'root' })
export class ComplaintService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/complaints`;

  getMy(status?: ComplaintStatus | null): Observable<Complaint[]> {
    let params = new HttpParams();

    if (status != null) {
      params = params.set('status', status);
    }

    return this.http.get<Complaint[]>(`${this.baseUrl}/my`, { params });
  }

  getById(id: string): Observable<Complaint> {
    return this.http.get<Complaint>(`${this.baseUrl}/${id}`);
  }

  create(body: CreateComplaintRequest): Observable<Complaint> {
    return this.http.post<Complaint>(this.baseUrl, body);
  }
}
