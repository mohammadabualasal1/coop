import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AddBranchOfferRequest,
  BranchOffer,
  Offer,
  OfferRequest,
  UpdateBranchOfferRequest
} from '../models/offer.models';

@Injectable({ providedIn: 'root' })
export class OfferService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/offers`;

  getMy(): Observable<Offer[]> {
    return this.http.get<Offer[]>(`${this.baseUrl}/my`);
  }

  getById(id: string): Observable<Offer> {
    return this.http.get<Offer>(`${this.baseUrl}/${id}`);
  }

  create(body: OfferRequest): Observable<Offer> {
    return this.http.post<Offer>(this.baseUrl, body);
  }

  update(id: string, body: OfferRequest): Observable<Offer> {
    return this.http.put<Offer>(`${this.baseUrl}/${id}`, body);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  submit(id: string): Observable<Offer> {
    return this.http.post<Offer>(`${this.baseUrl}/${id}/submit`, null);
  }

  pause(id: string): Observable<Offer> {
    return this.http.post<Offer>(`${this.baseUrl}/${id}/pause`, null);
  }

  resume(id: string): Observable<Offer> {
    return this.http.post<Offer>(`${this.baseUrl}/${id}/resume`, null);
  }

  cancel(id: string): Observable<Offer> {
    return this.http.post<Offer>(`${this.baseUrl}/${id}/cancel`, null);
  }

  addBranch(offerId: string, body: AddBranchOfferRequest): Observable<BranchOffer> {
    return this.http.post<BranchOffer>(`${this.baseUrl}/${offerId}/branches`, body);
  }

  updateBranchStock(
    offerId: string,
    branchOfferId: string,
    body: UpdateBranchOfferRequest
  ): Observable<BranchOffer> {
    return this.http.put<BranchOffer>(`${this.baseUrl}/${offerId}/branches/${branchOfferId}`, body);
  }

  removeBranch(offerId: string, branchOfferId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${offerId}/branches/${branchOfferId}`);
  }
}
