import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardStats, StockSummary, RecentActivity, DailySalesData } from '../models/dashboard.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly API_URL = `${environment.apiUrl}/dashboard`;

  constructor(private http: HttpClient) {}

  getStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.API_URL}/stats`);
  }

  getStockSummary(): Observable<StockSummary> {
    return this.http.get<StockSummary>(`${this.API_URL}/stock-summary`);
  }

  getRecentActivity(count: number = 5): Observable<RecentActivity[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<RecentActivity[]>(`${this.API_URL}/recent-activity`, { params });
  }

  getSalesCostsData(days: number = 30): Observable<DailySalesData[]> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<DailySalesData[]>(`${this.API_URL}/sales-costs`, { params });
  }
}
