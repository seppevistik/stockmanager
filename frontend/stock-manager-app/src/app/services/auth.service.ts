import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import {
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  User,
  UserRole,
  UserProfile,
  UpdateUserProfileRequest,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  RefreshTokenRequest,
  RefreshTokenResponse
} from '../models/user.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = environment.apiUrl;
  private readonly TOKEN_KEY = 'auth_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';
  private readonly EXPIRES_AT_KEY = 'token_expires_at';
  private readonly USER_KEY = 'current_user';

  private currentUserSubject = new BehaviorSubject<User | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/auth/login`, credentials)
      .pipe(
        tap(response => this.handleAuthResponse(response))
      );
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/auth/register`, data)
      .pipe(
        tap(response => this.handleAuthResponse(response))
      );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.EXPIRES_AT_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  setCurrentUser(user: User): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  hasRole(roles: UserRole[]): boolean {
    const user = this.getCurrentUser();
    return !!user && roles.includes(user.role);
  }

  getUserProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.API_URL}/auth/me`);
  }

  updateUserProfile(data: UpdateUserProfileRequest): Observable<any> {
    return this.http.put(`${this.API_URL}/auth/me`, data);
  }

  changePassword(data: ChangePasswordRequest): Observable<any> {
    return this.http.post(`${this.API_URL}/auth/change-password`, data);
  }

  forgotPassword(data: ForgotPasswordRequest): Observable<any> {
    return this.http.post(`${this.API_URL}/auth/forgot-password`, data);
  }

  resetPassword(data: ResetPasswordRequest): Observable<any> {
    return this.http.post(`${this.API_URL}/auth/reset-password`, data);
  }

  refreshToken(): Observable<RefreshTokenResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const request: RefreshTokenRequest = { refreshToken };
    return this.http.post<RefreshTokenResponse>(`${this.API_URL}/auth/refresh-token`, request)
      .pipe(
        tap(response => {
          // Update tokens in storage
          localStorage.setItem(this.TOKEN_KEY, response.token);
          localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
          localStorage.setItem(this.EXPIRES_AT_KEY, response.expiresAt.toString());

          // Update current user with new token
          const currentUser = this.getCurrentUser();
          if (currentUser) {
            currentUser.token = response.token;
            currentUser.refreshToken = response.refreshToken;
            currentUser.expiresAt = response.expiresAt;
            localStorage.setItem(this.USER_KEY, JSON.stringify(currentUser));
            this.currentUserSubject.next(currentUser);
          }
        })
      );
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  isTokenExpired(): boolean {
    const expiresAt = localStorage.getItem(this.EXPIRES_AT_KEY);
    if (!expiresAt) {
      return true;
    }

    const expirationDate = new Date(expiresAt);
    const now = new Date();

    // Consider token expired 1 minute before actual expiration for buffer
    return expirationDate.getTime() - now.getTime() < 60000;
  }

  getTokenExpirationTime(): Date | null {
    const expiresAt = localStorage.getItem(this.EXPIRES_AT_KEY);
    return expiresAt ? new Date(expiresAt) : null;
  }

  private handleAuthResponse(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(this.EXPIRES_AT_KEY, response.expiresAt.toString());
    localStorage.setItem(this.USER_KEY, JSON.stringify(response));
    this.currentUserSubject.next(response);
  }

  private getUserFromStorage(): User | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    return userJson ? JSON.parse(userJson) : null;
  }
}
