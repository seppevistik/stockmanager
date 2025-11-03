import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Company, CreateCompanyRequest } from '../models/company.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {
  private readonly API_URL = `${environment.apiUrl}/companies`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Company[]> {
    return this.http.get<Company[]>(this.API_URL);
  }

  getSuppliers(): Observable<Company[]> {
    return this.http.get<Company[]>(`${this.API_URL}/suppliers`);
  }

  getCustomers(): Observable<Company[]> {
    return this.http.get<Company[]>(`${this.API_URL}/customers`);
  }

  getById(id: number): Observable<Company> {
    return this.http.get<Company>(`${this.API_URL}/${id}`);
  }

  create(company: CreateCompanyRequest): Observable<Company> {
    return this.http.post<Company>(this.API_URL, company);
  }

  update(id: number, company: CreateCompanyRequest): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/${id}`, company);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }
}
