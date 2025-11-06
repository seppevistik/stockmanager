import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  UserDto,
  CreateUserRequest,
  UpdateUserRequest,
  UserListQuery,
  PagedResult,
  UserStatistics,
  ResetUserPasswordRequest
} from '../models/user-management.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserManagementService {
  private readonly API_URL = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getUsers(query: UserListQuery): Observable<PagedResult<UserDto>> {
    let params = new HttpParams()
      .set('page', query.page.toString())
      .set('pageSize', query.pageSize.toString());

    if (query.searchTerm) {
      params = params.set('searchTerm', query.searchTerm);
    }

    if (query.role !== undefined) {
      params = params.set('role', query.role.toString());
    }

    if (query.isActive !== undefined) {
      params = params.set('isActive', query.isActive.toString());
    }

    return this.http.get<PagedResult<UserDto>>(this.API_URL, { params });
  }

  getUser(id: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.API_URL}/${id}`);
  }

  createUser(request: CreateUserRequest): Observable<UserDto> {
    return this.http.post<UserDto>(this.API_URL, request);
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<any> {
    return this.http.put(`${this.API_URL}/${id}`, request);
  }

  toggleUserStatus(id: string): Observable<any> {
    return this.http.post(`${this.API_URL}/${id}/toggle-status`, {});
  }

  resetUserPassword(id: string, request: ResetUserPasswordRequest): Observable<any> {
    return this.http.post(`${this.API_URL}/${id}/reset-password`, request);
  }

  revokeUserSessions(id: string): Observable<any> {
    return this.http.post(`${this.API_URL}/${id}/revoke-sessions`, {});
  }

  getStatistics(): Observable<UserStatistics> {
    return this.http.get<UserStatistics>(`${this.API_URL}/statistics`);
  }
}
