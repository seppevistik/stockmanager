export interface Customer {
  id: number;
  name: string;
  isCompany: boolean;
  contactPerson?: string;
  email?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  country?: string;
  postalCode?: string;
  companyName?: string;
  taxNumber?: string;
  website?: string;
  creditLimit?: number;
  paymentTermsDays?: number;
  paymentMethod?: string;
  notes?: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt?: Date;
}

export interface CreateCustomerDto {
  name: string;
  isCompany: boolean;
  contactPerson?: string;
  email?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  country?: string;
  postalCode?: string;
  companyName?: string;
  taxNumber?: string;
  website?: string;
  creditLimit?: number;
  paymentTermsDays?: number;
  paymentMethod?: string;
  notes?: string;
  isActive: boolean;
}

export interface UpdateCustomerDto {
  name: string;
  isCompany: boolean;
  contactPerson?: string;
  email?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  country?: string;
  postalCode?: string;
  companyName?: string;
  taxNumber?: string;
  website?: string;
  creditLimit?: number;
  paymentTermsDays?: number;
  paymentMethod?: string;
  notes?: string;
  isActive: boolean;
}

export interface CustomerListQuery {
  searchTerm?: string;
  isCompany?: boolean;
  isActive?: boolean;
  country?: string;
  sortBy?: string;
  sortDirection?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedCustomerResult {
  items: Customer[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
