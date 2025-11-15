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
}
