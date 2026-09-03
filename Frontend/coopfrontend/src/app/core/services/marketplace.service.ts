import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  MerchantSummary,
  OfferDetail,
  OfferSummary,
  PagedResponse,
  SearchMerchantsParams,
  SearchOffersParams
} from '../models/marketplace.models';

function buildParams(values: Record<string, string | number | null | undefined>): HttpParams {
  let params = new HttpParams();

  for (const [key, value] of Object.entries(values)) {
    if (value === null || value === undefined || value === '') {
      continue;
    }

    params = params.set(key, value);
  }

  return params;
}

@Injectable({ providedIn: 'root' })
export class MarketplaceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/marketplace`;

  searchOffers(params: SearchOffersParams): Observable<PagedResponse<OfferSummary>> {
    return this.http.get<PagedResponse<OfferSummary>>(`${this.baseUrl}/offers`, {
      params: buildParams({ ...params })
    });
  }

  nearbyOffers(lat: number, lng: number, radiusKm: number): Observable<OfferSummary[]> {
    return this.http.get<OfferSummary[]>(`${this.baseUrl}/offers/nearby`, {
      params: buildParams({ latitude: lat, longitude: lng, radiusKm })
    });
  }

  endingSoon(): Observable<OfferSummary[]> {
    return this.http.get<OfferSummary[]>(`${this.baseUrl}/offers/ending-soon`);
  }

  topDiscounts(): Observable<OfferSummary[]> {
    return this.http.get<OfferSummary[]>(`${this.baseUrl}/offers/top-discounts`);
  }

  getOffer(id: string): Observable<OfferDetail> {
    return this.http.get<OfferDetail>(`${this.baseUrl}/offers/${id}`);
  }

  searchMerchants(params: SearchMerchantsParams): Observable<PagedResponse<MerchantSummary>> {
    return this.http.get<PagedResponse<MerchantSummary>>(`${this.baseUrl}/merchants`, {
      params: buildParams({ ...params })
    });
  }

  getMerchant(id: string): Observable<MerchantSummary> {
    return this.http.get<MerchantSummary>(`${this.baseUrl}/merchants/${id}`);
  }

  getMerchantOffers(id: string): Observable<OfferSummary[]> {
    return this.http.get<OfferSummary[]>(`${this.baseUrl}/merchants/${id}/offers`);
  }
}
