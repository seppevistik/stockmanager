import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Customer,
  CreateCustomerDto,
  UpdateCustomerDto,
  CustomerListQuery,
  PagedCustomerResult
} from '../models/customer.model';

@Injectable({
  providedIn: 'root'
})
export class CustomerService {
  private apiUrl = `${environment.apiUrl}/customers`;

  constructor(private http: HttpClient) { }

  getCustomers(query?: CustomerListQuery): Observable<PagedCustomerResult> {
    let params = new HttpParams();

    if (query) {
      if (query.searchTerm) {
        params = params.set('searchTerm', query.searchTerm);
      }
      if (query.isCompany !== undefined) {
        params = params.set('isCompany', query.isCompany.toString());
      }
      if (query.isActive !== undefined) {
        params = params.set('isActive', query.isActive.toString());
      }
      if (query.country) {
        params = params.set('country', query.country);
      }
      if (query.sortBy) {
        params = params.set('sortBy', query.sortBy);
      }
      if (query.sortDirection) {
        params = params.set('sortDirection', query.sortDirection);
      }
      if (query.page) {
        params = params.set('page', query.page.toString());
      }
      if (query.pageSize) {
        params = params.set('pageSize', query.pageSize.toString());
      }
    }

    return this.http.get<PagedCustomerResult>(this.apiUrl, { params });
  }

  getCustomerById(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${this.apiUrl}/${id}`);
  }

  createCustomer(customer: CreateCustomerDto): Observable<Customer> {
    return this.http.post<Customer>(this.apiUrl, customer);
  }

  updateCustomer(id: number, customer: UpdateCustomerDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, customer);
  }

  deleteCustomer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
