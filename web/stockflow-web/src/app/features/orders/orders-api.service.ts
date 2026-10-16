import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Order, PagedResult, PlaceOrder } from '../../core/api.models';

@Injectable({ providedIn: 'root' })
export class OrdersApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/orders`;

  search(status: string | null, page: number, pageSize: number): Observable<PagedResult<Order>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<PagedResult<Order>>(this.base, { params });
  }

  place(order: PlaceOrder): Observable<Order> {
    return this.http.post<Order>(this.base, order);
  }

  cancel(id: number): Observable<Order> {
    return this.http.post<Order>(`${this.base}/${id}/cancel`, {});
  }
}