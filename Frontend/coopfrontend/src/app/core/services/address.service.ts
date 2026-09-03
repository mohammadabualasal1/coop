import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Address, AddressRequest } from '../models/address.models';

@Injectable({ providedIn: 'root' })
export class AddressService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/addresses`;

  getAll(): Observable<Address[]> {
    return this.http.get<Address[]>(this.baseUrl);
  }

  create(body: AddressRequest): Observable<Address> {
    return this.http.post<Address>(this.baseUrl, body);
  }

  update(id: string, body: AddressRequest): Observable<Address> {
    return this.http.put<Address>(`${this.baseUrl}/${id}`, body);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  setDefault(id: string): Observable<Address> {
    return this.http.put<Address>(`${this.baseUrl}/${id}/set-default`, null);
  }
}
