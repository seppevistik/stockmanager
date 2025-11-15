import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { BusinessService, UserBusinessDto } from '../../../services/business.service';
import { User } from '../../../models/user.model';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';

interface MenuItem {
  path?: string;
  icon: string;
  label: string;
  roles: number[];
  children?: MenuItem[];
}

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
    MatMenuModule,
    MatExpansionModule,
    MatDividerModule
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements OnInit {
  currentUser: User | null = null;
  userBusinesses: UserBusinessDto[] = [];
  loadingBusinesses = false;

  menuGroups: MenuItem[] = [
    {
      icon: 'dashboard',
      label: 'Dashboard',
      path: '/dashboard',
      roles: []
    },
    {
      icon: 'inventory',
      label: 'Inventory',
      roles: [],
      children: [
        { path: '/products', icon: 'inventory_2', label: 'Products', roles: [] },
        { path: '/stock-movements', icon: 'swap_horiz', label: 'Stock Movements', roles: [] }
      ]
    },
    {
      icon: 'shopping_cart',
      label: 'Purchase',
      roles: [],
      children: [
        { path: '/purchase-orders', icon: 'shopping_cart', label: 'Purchase Orders', roles: [] },
        { path: '/receipts', icon: 'receipt_long', label: 'Receipts', roles: [] }
      ]
    },
    {
      icon: 'point_of_sale',
      label: 'Sales',
      roles: [],
      children: [
        { path: '/sales-orders', icon: 'point_of_sale', label: 'Sales Orders', roles: [] }
      ]
    },
    {
      icon: 'business',
      label: 'Business',
      roles: [],
      children: [
        { path: '/customers', icon: 'people', label: 'Customers', roles: [] },
        { path: '/companies', icon: 'business', label: 'Companies', roles: [] }
      ]
    }
  ];

  constructor(
    private authService: AuthService,
    private businessService: BusinessService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user && user.businessId) {
        this.loadUserBusinesses();
      }
    });
  }

  loadUserBusinesses(): void {
    this.loadingBusinesses = true;
    this.businessService.getMyBusinesses().subscribe({
      next: (businesses) => {
        this.userBusinesses = businesses;
        this.loadingBusinesses = false;
      },
      error: (error) => {
        console.error('Error loading user businesses:', error);
        this.loadingBusinesses = false;
      }
    });
  }

  switchBusiness(businessId: number): void {
    if (!this.currentUser || this.currentUser.businessId === businessId) {
      return;
    }

    this.businessService.switchBusiness(businessId).subscribe({
      next: (response) => {
        // Update the auth token with new business context
        localStorage.setItem('access_token', response.token);
        if (response.refreshToken) {
          localStorage.setItem('refresh_token', response.refreshToken);
        }

        // Update current user
        this.authService.setCurrentUser({
          userId: response.userId,
          email: response.email,
          firstName: response.firstName,
          lastName: response.lastName,
          role: parseInt(response.role),
          businessId: response.businessId,
          businessName: response.businessName
        });

        // Reload the page to refresh all business-scoped data
        window.location.reload();
      },
      error: (error) => {
        console.error('Error switching business:', error);
        alert('Failed to switch business. Please try again.');
      }
    });
  }

  shouldShowMenuItem(item: MenuItem): boolean {
    if (!this.currentUser) return false;
    if (item.roles.length === 0) return true; // Available to all roles
    return item.roles.includes(this.currentUser.role);
  }

  shouldShowMenuGroup(group: MenuItem): boolean {
    if (!this.currentUser) return false;

    // If the group itself has role restrictions, check them
    if (group.roles.length > 0 && !group.roles.includes(this.currentUser.role)) {
      return false;
    }

    // If the group has children, show it if at least one child is visible
    if (group.children && group.children.length > 0) {
      return group.children.some(child => this.shouldShowMenuItem(child));
    }

    // If it's a standalone item (has a path), check its permissions
    if (group.path) {
      return this.shouldShowMenuItem(group);
    }

    return true;
  }

  logout(): void {
    this.authService.logout();
  }
}
