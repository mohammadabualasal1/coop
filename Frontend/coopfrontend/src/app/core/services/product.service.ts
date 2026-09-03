import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Product,
  ProductImage,
  ProductImageRequest,
  ProductRequest,
  UpdateProductRequest
} from '../models/product.models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/products`;

  getMy(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/my`);
  }

  getById(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  create(body: ProductRequest): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, body);
  }

  update(id: string, body: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, body);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  addImage(id: string, body: ProductImageRequest): Observable<ProductImage> {
    return this.http.post<ProductImage>(`${this.baseUrl}/${id}/images`, body);
  }

  removeImage(id: string, imageId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/images/${imageId}`);
  }
}
