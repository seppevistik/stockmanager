import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StockMovement, CreateStockMovementRequest } from '../models/stock-movement.model';

@Injectable({
  providedIn: 'root'
})
export class StockMovementService {
  private readonly API_URL = 'http://localhost:5000/api/stockmovements';

  constructor(private http: HttpClient) {}

  getAll(startDate?: Date, endDate?: Date): Observable<StockMovement[]> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());

    return this.http.get<StockMovement[]>(this.API_URL, { params });
  }

  getByProduct(productId: number): Observable<StockMovement[]> {
    return this.http.get<StockMovement[]>(`${this.API_URL}/product/${productId}`);
  }

  getRecent(count: number = 10): Observable<StockMovement[]> {
    return this.http.get<StockMovement[]>(`${this.API_URL}/recent`, {
      params: { count: count.toString() }
    });
  }

  create(movement: CreateStockMovementRequest): Observable<StockMovement> {
    return this.http.post<StockMovement>(this.API_URL, movement);
  }
}
