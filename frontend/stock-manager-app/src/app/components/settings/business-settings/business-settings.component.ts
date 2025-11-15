import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatChipsModule } from '@angular/material/chips';
import { MatSelectModule } from '@angular/material/select';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UserManagementService } from '../../../services/user-management.service';
import { AuthService } from '../../../services/auth.service';
import { BusinessService, BusinessDto, UpdateBusinessDto } from '../../../services/business.service';
import { UserDto, CreateUserRequest, UpdateUserRequest, UserStatistics } from '../../../models/user-management.model';
import { CreateUserDialogComponent } from './create-user-dialog/create-user-dialog.component';
import { EditUserDialogComponent } from './edit-user-dialog/edit-user-dialog.component';

@Component({
  selector: 'app-business-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatDividerModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatChipsModule,
    MatSelectModule,
    MatDialogModule,
    MatTooltipModule
  ],
  templateUrl: './business-settings.component.html',
  styleUrls: ['./business-settings.component.scss']
})
export class BusinessSettingsComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns: string[] = ['name', 'email', 'role', 'status', 'lastLogin', 'actions'];
  dataSource: MatTableDataSource<UserDto>;
  users: UserDto[] = [];
  statistics: UserStatistics | null = null;
  isLoadingUsers = false;
  searchTerm = '';
  currentUser: any;

  businessForm: FormGroup;
  isEditingBusiness = false;
  isAdmin = false;

  constructor(
    private fb: FormBuilder,
    private userManagementService: UserManagementService,
    private authService: AuthService,
    private businessService: BusinessService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {
    this.dataSource = new MatTableDataSource<UserDto>([]);

    this.businessForm = this.fb.group({
      businessName: ['', Validators.required],
      contactEmail: ['', [Validators.required, Validators.email]],
      contactPhone: [''],
      address: [''],
      city: [''],
      country: [''],
      postalCode: [''],
      taxNumber: ['']
    });
  }

  ngOnInit(): void {
    this.currentUser = this.authService.getCurrentUser();
    this.isAdmin = this.authService.hasRole([0]); // Admin only
    this.loadUsers();
    this.loadStatistics();
    this.loadBusinessSettings();
  }

  loadUsers(): void {
    this.isLoadingUsers = true;
    this.userManagementService.getUsers({
      page: 1,
      pageSize: 100,
      searchTerm: this.searchTerm || undefined
    }).subscribe({
      next: (result) => {
        this.users = result.items;
        this.dataSource = new MatTableDataSource(this.users);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        this.isLoadingUsers = false;
      },
      error: (error) => {
        this.snackBar.open('Failed to load users', 'Close', { duration: 3000 });
        console.error('Error loading users:', error);
        this.isLoadingUsers = false;
      }
    });
  }

  loadStatistics(): void {
    this.userManagementService.getStatistics().subscribe({
      next: (stats) => {
        this.statistics = stats;
      },
      error: (error) => {
        console.error('Error loading statistics:', error);
      }
    });
  }

  loadBusinessSettings(): void {
    this.businessService.getBusiness().subscribe({
      next: (business) => {
        this.businessForm.patchValue({
          businessName: business.name,
          contactEmail: business.contactEmail || '',
          contactPhone: business.contactPhone || '',
          address: business.address || '',
          city: business.city || '',
          country: business.country || '',
          postalCode: business.postalCode || '',
          taxNumber: business.taxNumber || ''
        });
      },
      error: (error) => {
        console.error('Error loading business settings:', error);
        // Fallback to current user data
        const user = this.authService.getCurrentUser();
        if (user) {
          this.businessForm.patchValue({
            businessName: user.businessName || ''
          });
        }
      }
    });
  }

  applyFilter(): void {
    this.loadUsers();
  }

  openCreateUserDialog(): void {
    const dialogRef = this.dialog.open(CreateUserDialogComponent, {
      width: '550px',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.createUser(result);
      }
    });
  }

  createUser(userData: CreateUserRequest): void {
    this.userManagementService.createUser(userData).subscribe({
      next: () => {
        this.snackBar.open('User created successfully', 'Close', { duration: 3000 });
        this.loadUsers();
        this.loadStatistics();
      },
      error: (error) => {
        const message = error.error?.message || 'Failed to create user';
        this.snackBar.open(message, 'Close', { duration: 3000 });
        console.error('Error creating user:', error);
      }
    });
  }

  openEditUserDialog(user: UserDto): void {
    const dialogRef = this.dialog.open(EditUserDialogComponent, {
      width: '550px',
      disableClose: false,
      data: { user }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.updateUser(user.id, result);
      }
    });
  }

  updateUser(userId: string, userData: UpdateUserRequest): void {
    this.userManagementService.updateUser(userId, userData).subscribe({
      next: () => {
        this.snackBar.open('User updated successfully', 'Close', { duration: 3000 });
        this.loadUsers();
      },
      error: (error) => {
        const message = error.error?.message || 'Failed to update user';
        this.snackBar.open(message, 'Close', { duration: 3000 });
        console.error('Error updating user:', error);
      }
    });
  }

  toggleUserStatus(user: UserDto): void {
    const action = user.isActive ? 'deactivate' : 'activate';
    if (confirm(`Are you sure you want to ${action} this user?`)) {
      this.userManagementService.toggleUserStatus(user.id).subscribe({
        next: () => {
          this.snackBar.open(`User ${action}d successfully`, 'Close', { duration: 3000 });
          this.loadUsers();
          this.loadStatistics();
        },
        error: (error) => {
          const message = error.error?.message || `Failed to ${action} user`;
          this.snackBar.open(message, 'Close', { duration: 3000 });
          console.error(`Error ${action}ing user:`, error);
        }
      });
    }
  }

  revokeUserSessions(user: UserDto): void {
    if (confirm('Are you sure you want to revoke all sessions for this user? They will be logged out from all devices.')) {
      this.userManagementService.revokeUserSessions(user.id).subscribe({
        next: () => {
          this.snackBar.open('User sessions revoked successfully', 'Close', { duration: 3000 });
          this.loadUsers();
        },
        error: (error) => {
          const message = error.error?.message || 'Failed to revoke user sessions';
          this.snackBar.open(message, 'Close', { duration: 3000 });
          console.error('Error revoking user sessions:', error);
        }
      });
    }
  }

  getRoleClass(role: number): string {
    const roleClasses = ['role-admin', 'role-manager', 'role-staff', 'role-viewer'];
    return roleClasses[role] || 'role-viewer';
  }

  formatDate(date: Date | undefined): string {
    if (!date) return 'Never';
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  editBusinessSettings(): void {
    this.isEditingBusiness = true;
  }

  cancelBusinessEdit(): void {
    this.isEditingBusiness = false;
    this.loadBusinessSettings();
  }

  saveBusinessSettings(): void {
    if (this.businessForm.valid) {
      const updateDto: UpdateBusinessDto = {
        name: this.businessForm.value.businessName,
        contactEmail: this.businessForm.value.contactEmail,
        contactPhone: this.businessForm.value.contactPhone,
        address: this.businessForm.value.address,
        city: this.businessForm.value.city,
        country: this.businessForm.value.country,
        postalCode: this.businessForm.value.postalCode,
        taxNumber: this.businessForm.value.taxNumber
      };

      this.businessService.updateBusiness(updateDto).subscribe({
        next: () => {
          this.snackBar.open('Business settings saved successfully', 'Close', { duration: 3000 });
          this.isEditingBusiness = false;
          this.loadBusinessSettings();
        },
        error: (error) => {
          const message = error.error?.message || 'Failed to save business settings';
          this.snackBar.open(message, 'Close', { duration: 3000 });
          console.error('Error saving business settings:', error);
        }
      });
    }
  }
}
