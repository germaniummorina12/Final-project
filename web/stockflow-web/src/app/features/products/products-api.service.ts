import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AdjustStock, CreateProduct, PagedResult, Product, ProductQuery, UpdateProduct } from '../../core/api.models';

@Injectable({ providedIn: 'root' })
export class ProductsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/products`;

  search(query: ProductQuery): Observable<PagedResult<Product>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.categoryId) {
      params = params.set('categoryId', query.categoryId);
    }
    if (query.lowStockOnly) {
      params = params.set('lowStockOnly', true);
    }
    if (query.sort) {
      params = params.set('sort', query.sort);
    }
    return this.http.get<PagedResult<Product>>(this.base, { params });
  }

  lowStock(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.base}/low-stock`);
  }

  get(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.base}/${id}`);
  }

  create(body: CreateProduct): Observable<Product> {
    return this.http.post<Product>(this.base, body);
  }

  update(id: number, body: UpdateProduct): Observable<Product> {
    return this.http.put<Product>(`${this.base}/${id}`, body);
  }

  adjustStock(id: number, body: AdjustStock): Observable<Product> {
    return this.http.post<Product>(`${this.base}/${id}/stock`, body);
  }

  deactivate(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}