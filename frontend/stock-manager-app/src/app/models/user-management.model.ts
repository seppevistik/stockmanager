export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
  roleName: string;
  businessId: number;
  businessName: string;
  isActive: boolean;
  createdAt: Date;
  lastLoginAt?: Date;
  activeSessionCount: number;
}

export interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  role: number;
  temporaryPassword: string;
  sendWelcomeEmail: boolean;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  role: number;
  email: string;
}

export interface UserListQuery {
  searchTerm?: string;
  role?: number;
  isActive?: boolean;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserStatistics {
  Total: number;
  Active: number;
  Inactive: number;
  Admins: number;
  Managers: number;
  Staff: number;
  Viewers: number;
}

export interface ResetUserPasswordRequest {
  newPassword: string;
}
