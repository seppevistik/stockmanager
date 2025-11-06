import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { User } from '../../../models/user.model';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements OnInit {
  currentUser: User | null = null;

  menuItems = [
    { path: '/dashboard', icon: 'dashboard', label: 'Dashboard', roles: [] },
    { path: '/products', icon: 'inventory_2', label: 'Products', roles: [] },
    { path: '/stock-movements', icon: 'swap_horiz', label: 'Stock Movements', roles: [] },
    { path: '/companies', icon: 'business', label: 'Companies', roles: [] },
    { path: '/purchase-orders', icon: 'shopping_cart', label: 'Purchase Orders', roles: [] },
    { path: '/receipts', icon: 'receipt_long', label: 'Receipts', roles: [] },
    { path: '/sales-orders', icon: 'point_of_sale', label: 'Sales Orders', roles: [] },
    { path: '/users', icon: 'people', label: 'User Management', roles: [0] } // Admin only
  ];

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  shouldShowMenuItem(item: any): boolean {
    if (!this.currentUser) return false;
    if (item.roles.length === 0) return true; // Available to all roles
    return item.roles.includes(this.currentUser.role);
  }

  logout(): void {
    this.authService.logout();
  }
}
