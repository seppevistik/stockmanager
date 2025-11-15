import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface BusinessDto {
  id: number;
  name: string;
  description?: string;
  logoUrl?: string;
  contactEmail?: string;
  contactPhone?: string;
  address?: string;
  city?: string;
  country?: string;
  postalCode?: string;
  taxNumber?: string;
  createdAt: Date;
  updatedAt?: Date;
}

export interface UpdateBusinessDto {
  name: string;
  description?: string;
  contactEmail?: string;
  contactPhone?: string;
  address?: string;
  city?: string;
  country?: string;
  postalCode?: string;
  taxNumber?: string;
}

export interface CreateBusinessDto {
  name: string;
  description?: string;
  userRole: number;
}

export interface UserBusinessDto {
  businessId: number;
  businessName: string;
  role: string;
  isActive: boolean;
}

export interface SwitchBusinessResponse {
  token: string;
  refreshToken?: string;
  expiresAt: Date;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  businessId: number;
  businessName: string;
  businesses: UserBusinessDto[];
}

@Injectable({
  providedIn: 'root'
})
export class BusinessService {
  private readonly API_URL = `${environment.apiUrl}/business`;

  constructor(private http: HttpClient) {}

  getBusiness(): Observable<BusinessDto> {
    return this.http.get<BusinessDto>(this.API_URL);
  }

  updateBusiness(data: UpdateBusinessDto): Observable<void> {
    return this.http.put<void>(this.API_URL, data);
  }

  createBusiness(data: CreateBusinessDto): Observable<{ businessId: number }> {
    return this.http.post<{ businessId: number }>(`${this.API_URL}/create`, data);
  }

  getMyBusinesses(): Observable<UserBusinessDto[]> {
    return this.http.get<UserBusinessDto[]>(`${this.API_URL}/my-businesses`);
  }

  switchBusiness(businessId: number): Observable<SwitchBusinessResponse> {
    return this.http.post<SwitchBusinessResponse>(`${this.API_URL}/switch`, { businessId });
  }
}
