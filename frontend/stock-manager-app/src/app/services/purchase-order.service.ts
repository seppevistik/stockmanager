import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PurchaseOrder,
  CreatePurchaseOrderRequest,
  UpdatePurchaseOrderRequest,
  PurchaseOrderFilter,
  ConfirmPurchaseOrderRequest,
  CancelPurchaseOrderRequest
} from '../models/purchase-order.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PurchaseOrderService {
  private readonly API_URL = `${environment.apiUrl}/purchase-orders`;

  constructor(private http: HttpClient) {}

  getAll(filter?: PurchaseOrderFilter): Observable<PurchaseOrder[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.status) params = params.set('status', filter.status);
      if (filter.companyId) params = params.set('companyId', filter.companyId.toString());
      if (filter.fromDate) params = params.set('fromDate', filter.fromDate.toISOString());
      if (filter.toDate) params = params.set('toDate', filter.toDate.toISOString());
      if (filter.search) params = params.set('search', filter.search);
      if (filter.page) params = params.set('page', filter.page.toString());
      if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    }

    return this.http.get<PurchaseOrder[]>(this.API_URL, { params });
  }

  getOutstanding(): Observable<PurchaseOrder[]> {
    return this.http.get<PurchaseOrder[]>(`${this.API_URL}/outstanding`);
  }

  getById(id: number): Observable<PurchaseOrder> {
    return this.http.get<PurchaseOrder>(`${this.API_URL}/${id}`);
  }

  create(purchaseOrder: CreatePurchaseOrderRequest): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(this.API_URL, purchaseOrder);
  }

  update(id: number, purchaseOrder: UpdatePurchaseOrderRequest): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/${id}`, purchaseOrder);
  }

  submit(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/${id}/submit`, {});
  }

  confirm(id: number, request: ConfirmPurchaseOrderRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/${id}/confirm`, request);
  }

  cancel(id: number, request: CancelPurchaseOrderRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/${id}/cancel`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }
}
