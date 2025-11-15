import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { UserRole } from './models/user.model';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./components/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./components/auth/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./components/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent)
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./components/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./components/layout/main-layout/main-layout.component').then(m => m.MainLayoutComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./components/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./components/products/products-list/products-list.component').then(m => m.ProductsListComponent)
      },
      {
        path: 'products/new',
        loadComponent: () => import('./components/products/product-form/product-form.component').then(m => m.ProductFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager, UserRole.Staff] }
      },
      {
        path: 'products/edit/:id',
        loadComponent: () => import('./components/products/product-form/product-form.component').then(m => m.ProductFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager, UserRole.Staff] }
      },
      {
        path: 'products/bulk-adjust',
        loadComponent: () => import('./components/products/bulk-stock-adjustment/bulk-stock-adjustment.component').then(m => m.BulkStockAdjustmentComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager, UserRole.Staff] }
      },
      {
        path: 'stock-movements',
        loadComponent: () => import('./components/stock-movements/stock-movements-list/stock-movements-list.component').then(m => m.StockMovementsListComponent)
      },
      {
        path: 'companies',
        loadComponent: () => import('./components/companies/companies-list/companies-list.component').then(m => m.CompaniesListComponent)
      },
      {
        path: 'companies/new',
        loadComponent: () => import('./components/companies/company-form/company-form.component').then(m => m.CompanyFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager] }
      },
      {
        path: 'companies/edit/:id',
        loadComponent: () => import('./components/companies/company-form/company-form.component').then(m => m.CompanyFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager] }
      },
      {
        path: 'customers',
        loadComponent: () => import('./components/customers/customers-list/customers-list.component').then(m => m.CustomersListComponent)
      },
      {
        path: 'customers/new',
        loadComponent: () => import('./components/customers/customer-form/customer-form.component').then(m => m.CustomerFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager] }
      },
      {
        path: 'customers/edit/:id',
        loadComponent: () => import('./components/customers/customer-form/customer-form.component').then(m => m.CustomerFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager] }
      },
      {
        path: 'purchase-orders',
        loadComponent: () => import('./components/purchase-orders/purchase-orders-list/purchase-orders-list.component').then(m => m.PurchaseOrdersListComponent)
      },
      {
        path: 'purchase-orders/new',
        loadComponent: () => import('./components/purchase-orders/purchase-order-form/purchase-order-form.component').then(m => m.PurchaseOrderFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager] }
      },
      {
        path: 'purchase-orders/edit/:id',
        loadComponent: () => import('./components/purchase-orders/purchase-order-form/purchase-order-form.component').then(m => m.PurchaseOrderFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager] }
      },
      {
        path: 'purchase-orders/:id',
        loadComponent: () => import('./components/purchase-orders/purchase-order-detail/purchase-order-detail.component').then(m => m.PurchaseOrderDetailComponent)
      },
      {
        path: 'receipts',
        loadComponent: () => import('./components/receipts/receipts-list/receipts-list.component').then(m => m.ReceiptsListComponent)
      },
      {
        path: 'receipts/new',
        loadComponent: () => import('./components/receipts/receipt-form/receipt-form.component').then(m => m.ReceiptFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager, UserRole.Staff] }
      },
      {
        path: 'receipts/:id',
        loadComponent: () => import('./components/receipts/receipt-detail/receipt-detail.component').then(m => m.ReceiptDetailComponent)
      },
      {
        path: 'sales-orders',
        loadComponent: () => import('./components/sales-orders/sales-orders-list/sales-orders-list.component').then(m => m.SalesOrdersListComponent)
      },
      {
        path: 'sales-orders/new',
        loadComponent: () => import('./components/sales-orders/sales-order-form/sales-order-form.component').then(m => m.SalesOrderFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager] }
      },
      {
        path: 'sales-orders/edit/:id',
        loadComponent: () => import('./components/sales-orders/sales-order-form/sales-order-form.component').then(m => m.SalesOrderFormComponent),
        data: { roles: [UserRole.Admin, UserRole.Manager] }
      },
      {
        path: 'sales-orders/:id',
        loadComponent: () => import('./components/sales-orders/sales-order-detail/sales-order-detail.component').then(m => m.SalesOrderDetailComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./components/profile/user-profile.component').then(m => m.UserProfileComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./components/settings/settings.component').then(m => m.SettingsComponent)
      },
      {
        path: 'users',
        loadComponent: () => import('./components/users/users-list.component').then(m => m.UsersListComponent),
        data: { roles: [UserRole.Admin] }
      },
      {
        path: 'users/new',
        loadComponent: () => import('./components/users/user-form.component').then(m => m.UserFormComponent),
        data: { roles: [UserRole.Admin] }
      },
      {
        path: 'users/edit/:id',
        loadComponent: () => import('./components/users/user-form.component').then(m => m.UserFormComponent),
        data: { roles: [UserRole.Admin] }
      },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
