import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  SalesOrder,
  SalesOrderSummary,
  CreateSalesOrderRequest,
  UpdateSalesOrderRequest,
  SalesOrderListQuery,
  SalesOrderStatistics,
  PagedResult,
  SubmitOrderRequest,
  ConfirmOrderRequest,
  CancelOrderRequest,
  HoldOrderRequest,
  ReleaseOrderRequest,
  StartPickingRequest,
  CompletePickingRequest,
  StartPackingRequest,
  CompletePackingRequest,
  ShipOrderRequest,
  DeliverOrderRequest
} from '../models/sales-order.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SalesOrderService {
  private readonly API_URL = `${environment.apiUrl}/sales-orders`;

  constructor(private http: HttpClient) {}

  // List and Search
  getSalesOrders(query: SalesOrderListQuery): Observable<PagedResult<SalesOrderSummary>> {
    let params = new HttpParams();

    params = params.set('page', query.page.toString());
    params = params.set('pageSize', query.pageSize.toString());

    if (query.searchTerm) {
      params = params.set('searchTerm', query.searchTerm);
    }
    if (query.customerId) {
      params = params.set('customerId', query.customerId.toString());
    }
    if (query.status) {
      params = params.set('status', query.status);
    }
    if (query.priority) {
      params = params.set('priority', query.priority);
    }
    if (query.orderDateFrom) {
      params = params.set('orderDateFrom', query.orderDateFrom.toISOString());
    }
    if (query.orderDateTo) {
      params = params.set('orderDateTo', query.orderDateTo.toISOString());
    }
    if (query.requiredDateFrom) {
      params = params.set('requiredDateFrom', query.requiredDateFrom.toISOString());
    }
    if (query.requiredDateTo) {
      params = params.set('requiredDateTo', query.requiredDateTo.toISOString());
    }
    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }
    if (query.sortDirection) {
      params = params.set('sortDirection', query.sortDirection);
    }

    return this.http.get<PagedResult<SalesOrderSummary>>(this.API_URL, { params });
  }

  // Get by ID
  getSalesOrder(id: number): Observable<SalesOrder> {
    return this.http.get<SalesOrder>(`${this.API_URL}/${id}`);
  }

  // Create
  createSalesOrder(request: CreateSalesOrderRequest): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(this.API_URL, request);
  }

  // Update (only Draft)
  updateSalesOrder(id: number, request: UpdateSalesOrderRequest): Observable<SalesOrder> {
    return this.http.put<SalesOrder>(`${this.API_URL}/${id}`, request);
  }

  // Delete (only Draft)
  deleteSalesOrder(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }

  // Workflow: Submit Order (Draft → Submitted)
  submitOrder(id: number, request: SubmitOrderRequest = {}): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/submit`, request);
  }

  // Workflow: Confirm Order (Submitted → Confirmed)
  confirmOrder(id: number, request: ConfirmOrderRequest = {}): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/confirm`, request);
  }

  // Workflow: Cancel Order (any → Cancelled)
  cancelOrder(id: number, request: CancelOrderRequest): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/cancel`, request);
  }

  // Workflow: Hold Order (any → OnHold)
  holdOrder(id: number, request: HoldOrderRequest): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/hold`, request);
  }

  // Workflow: Release Order (OnHold → Confirmed)
  releaseOrder(id: number, request: ReleaseOrderRequest = {}): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/release`, request);
  }

  // Workflow: Start Picking (Confirmed → Picking)
  startPicking(id: number, request: StartPickingRequest = {}): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/start-picking`, request);
  }

  // Workflow: Complete Picking (Picking → Picked)
  completePicking(id: number, request: CompletePickingRequest): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/complete-picking`, request);
  }

  // Workflow: Start Packing (Picked → Packing)
  startPacking(id: number, request: StartPackingRequest = {}): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/start-packing`, request);
  }

  // Workflow: Complete Packing (Packing → Packed)
  completePacking(id: number, request: CompletePackingRequest = {}): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/complete-packing`, request);
  }

  // Workflow: Ship Order (Packed → Shipped)
  shipOrder(id: number, request: ShipOrderRequest): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/ship`, request);
  }

  // Workflow: Deliver Order (Shipped → Delivered)
  deliverOrder(id: number, request: DeliverOrderRequest): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.API_URL}/${id}/deliver`, request);
  }

  // Statistics
  getStatistics(): Observable<SalesOrderStatistics> {
    return this.http.get<SalesOrderStatistics>(`${this.API_URL}/statistics`);
  }
}
