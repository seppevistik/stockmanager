import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Receipt,
  CreateReceiptRequest,
  UpdateReceiptRequest,
  ReceiptValidation,
  ApproveReceiptRequest,
  RejectReceiptRequest
} from '../models/receipt.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ReceiptService {
  private readonly API_URL = `${environment.apiUrl}/receipts`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Receipt[]> {
    return this.http.get<Receipt[]>(this.API_URL);
  }

  getPendingValidation(): Observable<Receipt[]> {
    return this.http.get<Receipt[]>(`${this.API_URL}/pending-validation`);
  }

  getByPurchaseOrder(purchaseOrderId: number): Observable<Receipt[]> {
    return this.http.get<Receipt[]>(`${this.API_URL}/purchase-order/${purchaseOrderId}`);
  }

  getById(id: number): Observable<Receipt> {
    return this.http.get<Receipt>(`${this.API_URL}/${id}`);
  }

  create(receipt: CreateReceiptRequest): Observable<Receipt> {
    return this.http.post<Receipt>(this.API_URL, receipt);
  }

  update(id: number, receipt: UpdateReceiptRequest): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/${id}`, receipt);
  }

  validate(id: number): Observable<ReceiptValidation> {
    return this.http.get<ReceiptValidation>(`${this.API_URL}/${id}/validate`);
  }

  approve(id: number, request: ApproveReceiptRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/${id}/approve`, request);
  }

  reject(id: number, request: RejectReceiptRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/${id}/reject`, request);
  }

  complete(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/${id}/complete`, {});
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }
}
