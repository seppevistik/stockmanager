import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { UserManagementService } from '../../services/user-management.service';
import { CreateUserRequest, UpdateUserRequest, UserDto } from '../../models/user-management.model';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatChipsModule
  ],
  templateUrl: './user-form.component.html',
  styleUrl: './user-form.component.scss'
})
export class UserFormComponent implements OnInit {
  userForm: FormGroup;
  loading = false;
  saving = false;
  isEditMode = false;
  userId?: string;
  user?: UserDto;

  roles = [
    { value: 0, label: 'Admin', description: 'Full system access' },
    { value: 1, label: 'Manager', description: 'Manage products, orders, and staff' },
    { value: 2, label: 'Staff', description: 'Process orders and manage inventory' },
    { value: 3, label: 'Viewer', description: 'View-only access' }
  ];

  constructor(
    private fb: FormBuilder,
    private userService: UserManagementService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.userForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      role: [2, [Validators.required]],
      temporaryPassword: ['', [Validators.required, Validators.minLength(6)]],
      sendWelcomeEmail: [true]
    });
  }

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id') || undefined;
    this.isEditMode = !!this.userId;

    if (this.isEditMode && this.userId) {
      // Remove password field for edit mode
      this.userForm.removeControl('temporaryPassword');
      this.userForm.removeControl('sendWelcomeEmail');
      this.loadUser();
    }
  }

  loadUser(): void {
    if (!this.userId) return;

    this.loading = true;
    this.userService.getUser(this.userId).subscribe({
      next: (user) => {
        this.user = user;
        this.userForm.patchValue({
          email: user.email,
          firstName: user.firstName,
          lastName: user.lastName,
          role: user.role
        });
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading user:', error);
        this.snackBar.open('Error loading user', 'Close', { duration: 3000 });
        this.loading = false;
        this.router.navigate(['/users']);
      }
    });
  }

  onSubmit(): void {
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.saving = true;

    if (this.isEditMode && this.userId) {
      this.updateUser();
    } else {
      this.createUser();
    }
  }

  createUser(): void {
    const request: CreateUserRequest = this.userForm.value;

    this.userService.createUser(request).subscribe({
      next: (user) => {
        this.snackBar.open('User created successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/users']);
      },
      error: (error) => {
        console.error('Error creating user:', error);
        this.snackBar.open(error.error?.message || 'Error creating user', 'Close', { duration: 3000 });
        this.saving = false;
      }
    });
  }

  updateUser(): void {
    if (!this.userId) return;

    const request: UpdateUserRequest = this.userForm.value;

    this.userService.updateUser(this.userId, request).subscribe({
      next: () => {
        this.snackBar.open('User updated successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/users']);
      },
      error: (error) => {
        console.error('Error updating user:', error);
        this.snackBar.open(error.error?.message || 'Error updating user', 'Close', { duration: 3000 });
        this.saving = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/users']);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.userForm.get(fieldName);
    if (!field) return '';

    if (field.hasError('required')) {
      return `${this.getFieldLabel(fieldName)} is required`;
    }

    if (field.hasError('email')) {
      return 'Please enter a valid email';
    }

    if (field.hasError('minlength')) {
      const minLength = field.getError('minlength').requiredLength;
      return `Must be at least ${minLength} characters`;
    }

    return '';
  }

  getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      email: 'Email',
      firstName: 'First name',
      lastName: 'Last name',
      role: 'Role',
      temporaryPassword: 'Temporary password'
    };
    return labels[fieldName] || fieldName;
  }
}
