export enum UserRole {
  Admin = 0,
  Manager = 1,
  Staff = 2,
  Viewer = 3
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  businessName: string;
  role: UserRole;
}

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
  businessId: number;
  businessName: string;
}

export interface User extends AuthResponse {}
