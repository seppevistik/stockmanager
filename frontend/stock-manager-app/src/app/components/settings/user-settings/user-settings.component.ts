import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AuthService } from '../../../services/auth.service';
import { UserProfile, UpdateUserProfileRequest, ChangePasswordRequest } from '../../../models/user.model';

@Component({
  selector: 'app-user-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatDividerModule,
    MatSlideToggleModule
  ],
  templateUrl: './user-settings.component.html',
  styleUrls: ['./user-settings.component.scss']
})
export class UserSettingsComponent implements OnInit {
  profileForm: FormGroup;
  passwordForm: FormGroup;
  preferencesForm: FormGroup;
  userProfile: UserProfile | null = null;
  isEditingProfile = false;
  isChangingPassword = false;
  hideCurrentPassword = true;
  hideNewPassword = true;
  hideConfirmPassword = true;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private snackBar: MatSnackBar
  ) {
    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: [{ value: '', disabled: true }]
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });

    this.preferencesForm = this.fb.group({
      emailNotifications: [true],
      lowStockAlerts: [true],
      orderStatusUpdates: [true],
      weeklyReports: [false]
    });
  }

  ngOnInit(): void {
    this.loadUserProfile();
    this.loadPreferences();
  }

  loadUserProfile(): void {
    this.authService.getUserProfile().subscribe({
      next: (profile) => {
        this.userProfile = profile;
        this.profileForm.patchValue({
          firstName: profile.firstName,
          lastName: profile.lastName,
          email: profile.email
        });
      },
      error: (error) => {
        this.snackBar.open('Failed to load user profile', 'Close', { duration: 3000 });
        console.error('Error loading profile:', error);
      }
    });
  }

  loadPreferences(): void {
    // Load from localStorage for now
    const savedPreferences = localStorage.getItem('user_preferences');
    if (savedPreferences) {
      try {
        const prefs = JSON.parse(savedPreferences);
        this.preferencesForm.patchValue(prefs);
      } catch (e) {
        console.error('Error loading preferences:', e);
      }
    }
  }

  editProfile(): void {
    this.isEditingProfile = true;
  }

  cancelProfileEdit(): void {
    this.isEditingProfile = false;
    if (this.userProfile) {
      this.profileForm.patchValue({
        firstName: this.userProfile.firstName,
        lastName: this.userProfile.lastName
      });
    }
  }

  saveProfile(): void {
    if (this.profileForm.valid) {
      const updateData: UpdateUserProfileRequest = {
        firstName: this.profileForm.value.firstName,
        lastName: this.profileForm.value.lastName
      };

      this.authService.updateUserProfile(updateData).subscribe({
        next: () => {
          this.snackBar.open('Profile updated successfully', 'Close', { duration: 3000 });
          this.isEditingProfile = false;
          this.loadUserProfile();
        },
        error: (error) => {
          this.snackBar.open('Failed to update profile', 'Close', { duration: 3000 });
          console.error('Error updating profile:', error);
        }
      });
    }
  }

  startChangingPassword(): void {
    this.isChangingPassword = true;
  }

  cancelPasswordChange(): void {
    this.isChangingPassword = false;
    this.passwordForm.reset();
  }

  changePassword(): void {
    if (this.passwordForm.valid) {
      const passwordData: ChangePasswordRequest = {
        currentPassword: this.passwordForm.value.currentPassword,
        newPassword: this.passwordForm.value.newPassword,
        confirmPassword: this.passwordForm.value.confirmPassword
      };

      this.authService.changePassword(passwordData).subscribe({
        next: () => {
          this.snackBar.open('Password changed successfully', 'Close', { duration: 3000 });
          this.isChangingPassword = false;
          this.passwordForm.reset();
        },
        error: (error) => {
          const message = error.error?.message || 'Failed to change password';
          this.snackBar.open(message, 'Close', { duration: 3000 });
          console.error('Error changing password:', error);
        }
      });
    }
  }

  savePreferences(): void {
    // Save to localStorage for now
    const preferences = this.preferencesForm.value;
    localStorage.setItem('user_preferences', JSON.stringify(preferences));
    this.snackBar.open('Preferences saved successfully', 'Close', { duration: 3000 });
  }

  passwordMatchValidator(formGroup: FormGroup): { [key: string]: boolean } | null {
    const newPassword = formGroup.get('newPassword');
    const confirmPassword = formGroup.get('confirmPassword');

    if (!newPassword || !confirmPassword) {
      return null;
    }

    return newPassword.value === confirmPassword.value ? null : { passwordMismatch: true };
  }

  getRoleName(): string {
    if (!this.userProfile) return '';
    const roleNames = ['Admin', 'Manager', 'Staff', 'Viewer'];
    return roleNames[this.userProfile.role] || 'Unknown';
  }

  getAccountStatus(): string {
    return this.userProfile?.isActive ? 'Active' : 'Inactive';
  }

  formatDate(date: Date | undefined): string {
    if (!date) return 'Never';
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }
}
