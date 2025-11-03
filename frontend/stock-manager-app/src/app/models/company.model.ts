export interface Company {
  id: number;
  name: string;
  contactPerson?: string;
  email?: string;
  phone?: string;
  address?: string;
  city?: string;
  country?: string;
  postalCode?: string;
  website?: string;
  taxNumber?: string;
  isSupplier: boolean;
  isCustomer: boolean;
  notes?: string;
  createdAt: Date;
}

export interface CreateCompanyRequest {
  name: string;
  contactPerson?: string;
  email?: string;
  phone?: string;
  address?: string;
  city?: string;
  country?: string;
  postalCode?: string;
  website?: string;
  taxNumber?: string;
  isSupplier: boolean;
  isCustomer: boolean;
  notes?: string;
}
