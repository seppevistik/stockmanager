import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

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
        path: 'products/add',
        loadComponent: () => import('./components/products/product-form/product-form.component').then(m => m.ProductFormComponent),
        data: { roles: ['Admin', 'Manager', 'Staff'] }
      },
      {
        path: 'products/edit/:id',
        loadComponent: () => import('./components/products/product-form/product-form.component').then(m => m.ProductFormComponent),
        data: { roles: ['Admin', 'Manager', 'Staff'] }
      },
      {
        path: 'stock-movements',
        loadComponent: () => import('./components/stock-movements/stock-movements-list/stock-movements-list.component').then(m => m.StockMovementsListComponent)
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
