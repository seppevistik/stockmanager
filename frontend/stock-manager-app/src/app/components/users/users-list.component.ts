import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { UserManagementService } from '../../services/user-management.service';
import { UserDto, UserListQuery, UserStatistics } from '../../models/user-management.model';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule,
    MatTooltipModule
  ],
  templateUrl: './users-list.component.html',
  styleUrl: './users-list.component.scss'
})
export class UsersListComponent implements OnInit {
  displayedColumns: string[] = ['name', 'email', 'role', 'status', 'lastLogin', 'sessions', 'actions'];
  users: UserDto[] = [];
  statistics?: UserStatistics;
  loading = false;

  searchControl = new FormControl('');
  roleFilter = new FormControl<number | null>(null);
  statusFilter = new FormControl<boolean | null>(null);

  query: UserListQuery = {
    page: 1,
    pageSize: 10
  };

  totalCount = 0;
  pageSize = 10;
  pageIndex = 0;

  roles = [
    { value: 0, label: 'Admin' },
    { value: 1, label: 'Manager' },
    { value: 2, label: 'Staff' },
    { value: 3, label: 'Viewer' }
  ];

  constructor(
    private userService: UserManagementService,
    private router: Router,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.loadStatistics();
    this.setupFilters();
  }

  setupFilters(): void {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(value => {
        this.query.searchTerm = value || undefined;
        this.query.page = 1;
        this.pageIndex = 0;
        this.loadUsers();
      });

    this.roleFilter.valueChanges.subscribe(value => {
      this.query.role = value !== null ? value : undefined;
      this.query.page = 1;
      this.pageIndex = 0;
      this.loadUsers();
    });

    this.statusFilter.valueChanges.subscribe(value => {
      this.query.isActive = value !== null ? value : undefined;
      this.query.page = 1;
      this.pageIndex = 0;
      this.loadUsers();
    });
  }

  loadUsers(): void {
    this.loading = true;
    this.userService.getUsers(this.query).subscribe({
      next: (result) => {
        this.users = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.snackBar.open('Error loading users', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  loadStatistics(): void {
    this.userService.getStatistics().subscribe({
      next: (stats) => {
        this.statistics = stats;
      },
      error: (error) => {
        console.error('Error loading statistics:', error);
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.query.page = event.pageIndex + 1;
    this.query.pageSize = event.pageSize;
    this.pageSize = event.pageSize;
    this.pageIndex = event.pageIndex;
    this.loadUsers();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.roleFilter.setValue(null);
    this.statusFilter.setValue(null);
  }

  createUser(): void {
    this.router.navigate(['/users/new']);
  }

  editUser(user: UserDto): void {
    this.router.navigate(['/users/edit', user.id]);
  }

  toggleUserStatus(user: UserDto): void {
    const action = user.isActive ? 'deactivate' : 'activate';
    const confirmed = confirm(`Are you sure you want to ${action} ${user.firstName} ${user.lastName}?`);

    if (confirmed) {
      this.userService.toggleUserStatus(user.id).subscribe({
        next: () => {
          this.snackBar.open(`User ${action}d successfully`, 'Close', { duration: 3000 });
          this.loadUsers();
          this.loadStatistics();
        },
        error: (error) => {
          console.error('Error toggling user status:', error);
          this.snackBar.open(error.error?.message || `Error ${action}ing user`, 'Close', { duration: 3000 });
        }
      });
    }
  }

  revokeUserSessions(user: UserDto): void {
    const confirmed = confirm(`Are you sure you want to revoke all sessions for ${user.firstName} ${user.lastName}?`);

    if (confirmed) {
      this.userService.revokeUserSessions(user.id).subscribe({
        next: () => {
          this.snackBar.open('User sessions revoked successfully', 'Close', { duration: 3000 });
          this.loadUsers();
        },
        error: (error) => {
          console.error('Error revoking sessions:', error);
          this.snackBar.open(error.error?.message || 'Error revoking sessions', 'Close', { duration: 3000 });
        }
      });
    }
  }

  getRoleColor(role: number): string {
    const colors: { [key: number]: string } = {
      0: 'warn',     // Admin - red
      1: 'accent',   // Manager - accent
      2: 'primary',  // Staff - blue
      3: ''          // Viewer - default
    };
    return colors[role] || '';
  }

  getStatusColor(isActive: boolean): string {
    return isActive ? 'primary' : 'warn';
  }

  formatLastLogin(lastLoginAt?: Date): string {
    if (!lastLoginAt) {
      return 'Never';
    }
    const date = new Date(lastLoginAt);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;

    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;

    const diffDays = Math.floor(diffHours / 24);
    if (diffDays < 7) return `${diffDays}d ago`;

    return date.toLocaleDateString();
  }
}
