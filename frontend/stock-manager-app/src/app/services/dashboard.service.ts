import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardStats, StockSummary, RecentActivity } from '../models/dashboard.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly API_URL = 'http://localhost:5000/api/dashboard';

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
}
