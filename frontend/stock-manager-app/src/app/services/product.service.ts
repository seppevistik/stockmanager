import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product, CreateProductRequest, StockAdjustment, BulkStockAdjustment, CreateProductSupplier } from '../models/product.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly API_URL = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Product[]> {
    return this.http.get<Product[]>(this.API_URL);
  }

  getById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.API_URL}/${id}`);
  }

  getLowStock(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.API_URL}/low-stock`);
  }

  create(product: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(this.API_URL, product);
  }

  update(id: number, product: CreateProductRequest): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/${id}`, product);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }

  bulkAdjustStock(adjustments: BulkStockAdjustment): Observable<{ message: string; updatedCount: number }> {
    return this.http.post<{ message: string; updatedCount: number }>(`${this.API_URL}/bulk-adjust-stock`, adjustments);
  }

  addProductSupplier(productId: number, supplier: CreateProductSupplier): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/${productId}/suppliers`, supplier);
  }

  updateProductSupplier(productId: number, supplierLinkId: number, supplier: CreateProductSupplier): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/${productId}/suppliers/${supplierLinkId}`, supplier);
  }

  removeProductSupplier(productId: number, supplierLinkId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${productId}/suppliers/${supplierLinkId}`);
  }
}
