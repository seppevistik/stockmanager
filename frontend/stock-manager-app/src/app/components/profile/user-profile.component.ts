import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../services/auth.service';
import { UserProfile, UpdateUserProfileRequest } from '../../models/user.model';
import { ChangePasswordDialogComponent } from './change-password-dialog.component';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule,
    MatDividerModule
  ],
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.scss'
})
export class UserProfileComponent implements OnInit {
  profileForm: FormGroup;
  profile?: UserProfile;
  loading = false;
  saving = false;
  editMode = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]]
    });
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading = true;
    this.authService.getUserProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.profileForm.patchValue({
          firstName: profile.firstName,
          lastName: profile.lastName
        });
        this.profileForm.disable();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading profile:', error);
        this.snackBar.open('Error loading profile', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  enableEdit(): void {
    this.editMode = true;
    this.profileForm.enable();
  }

  cancelEdit(): void {
    this.editMode = false;
    this.profileForm.patchValue({
      firstName: this.profile?.firstName,
      lastName: this.profile?.lastName
    });
    this.profileForm.disable();
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      return;
    }

    this.saving = true;
    const updateData: UpdateUserProfileRequest = this.profileForm.value;

    this.authService.updateUserProfile(updateData).subscribe({
      next: () => {
        this.snackBar.open('Profile updated successfully', 'Close', { duration: 3000 });
        this.editMode = false;
        this.profileForm.disable();
        this.loadProfile();
        this.saving = false;
      },
      error: (error) => {
        console.error('Error updating profile:', error);
        this.snackBar.open(error.error?.message || 'Error updating profile', 'Close', { duration: 3000 });
        this.saving = false;
      }
    });
  }

  openChangePasswordDialog(): void {
    const dialogRef = this.dialog.open(ChangePasswordDialogComponent, {
      width: '450px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === true) {
        this.snackBar.open('Password changed successfully', 'Close', { duration: 3000 });
      }
    });
  }

  getRoleDisplayName(role: number): string {
    const roles: { [key: number]: string } = {
      0: 'Admin',
      1: 'Manager',
      2: 'Staff',
      3: 'Viewer'
    };
    return roles[role] || 'Unknown';
  }

  getStatusColor(isActive: boolean): string {
    return isActive ? 'primary' : 'warn';
  }
}
