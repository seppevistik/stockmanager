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
        path: 'purchase-orders',
        loadComponent: () => import('./components/purchase-orders/purchase-orders-list/purchase-orders-list.component').then(m => m.PurchaseOrdersListComponent)
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
