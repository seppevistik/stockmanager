import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../services/auth.service';
import { ChangePasswordRequest } from '../../models/user.model';

@Component({
  selector: 'app-change-password-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  template: `
    <h2 mat-dialog-title>Change Password</h2>
    <mat-dialog-content>
      <form [formGroup]="passwordForm">
        <mat-form-field class="full-width">
          <mat-label>Current Password</mat-label>
          <input matInput [type]="hideCurrentPassword ? 'password' : 'text'"
                 formControlName="currentPassword" placeholder="Enter current password">
          <button mat-icon-button matSuffix (click)="hideCurrentPassword = !hideCurrentPassword" type="button">
            <mat-icon>{{ hideCurrentPassword ? 'visibility' : 'visibility_off' }}</mat-icon>
          </button>
          <mat-error *ngIf="passwordForm.get('currentPassword')?.hasError('required')">
            Current password is required
          </mat-error>
        </mat-form-field>

        <mat-form-field class="full-width">
          <mat-label>New Password</mat-label>
          <input matInput [type]="hideNewPassword ? 'password' : 'text'"
                 formControlName="newPassword" placeholder="Enter new password">
          <button mat-icon-button matSuffix (click)="hideNewPassword = !hideNewPassword" type="button">
            <mat-icon>{{ hideNewPassword ? 'visibility' : 'visibility_off' }}</mat-icon>
          </button>
          <mat-hint>At least 6 characters with uppercase, lowercase, and digit</mat-hint>
          <mat-error *ngIf="passwordForm.get('newPassword')?.hasError('required')">
            New password is required
          </mat-error>
          <mat-error *ngIf="passwordForm.get('newPassword')?.hasError('minlength')">
            Password must be at least 6 characters
          </mat-error>
          <mat-error *ngIf="passwordForm.get('newPassword')?.hasError('pattern')">
            Password must contain uppercase, lowercase, and digit
          </mat-error>
        </mat-form-field>

        <mat-form-field class="full-width">
          <mat-label>Confirm New Password</mat-label>
          <input matInput [type]="hideConfirmPassword ? 'password' : 'text'"
                 formControlName="confirmPassword" placeholder="Confirm new password">
          <button mat-icon-button matSuffix (click)="hideConfirmPassword = !hideConfirmPassword" type="button">
            <mat-icon>{{ hideConfirmPassword ? 'visibility' : 'visibility_off' }}</mat-icon>
          </button>
          <mat-error *ngIf="passwordForm.get('confirmPassword')?.hasError('required')">
            Please confirm your password
          </mat-error>
          <mat-error *ngIf="passwordForm.get('confirmPassword')?.hasError('passwordMismatch')">
            Passwords do not match
          </mat-error>
        </mat-form-field>

        <div *ngIf="errorMessage" class="error-message">
          {{ errorMessage }}
        </div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close [disabled]="saving">Cancel</button>
      <button mat-raised-button color="primary" (click)="changePassword()"
              [disabled]="passwordForm.invalid || saving">
        <mat-spinner *ngIf="saving" diameter="20" class="inline-spinner"></mat-spinner>
        <span *ngIf="!saving">Change Password</span>
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }

    mat-dialog-content {
      min-width: 400px;
      padding-top: 20px;
    }

    .error-message {
      background-color: #ffebee;
      border: 1px solid #f44336;
      border-radius: 4px;
      padding: 12px;
      color: #c62828;
      margin-top: 8px;
      font-size: 14px;
    }

    .inline-spinner {
      display: inline-block;
      vertical-align: middle;
    }

    @media (max-width: 600px) {
      mat-dialog-content {
        min-width: 280px;
      }
    }
  `]
})
export class ChangePasswordDialogComponent {
  passwordForm: FormGroup;
  hideCurrentPassword = true;
  hideNewPassword = true;
  hideConfirmPassword = true;
  saving = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private dialogRef: MatDialogRef<ChangePasswordDialogComponent>
  ) {
    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/)
      ]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword');
    const confirmPassword = control.get('confirmPassword');

    if (!newPassword || !confirmPassword) {
      return null;
    }

    if (confirmPassword.value === '') {
      return null;
    }

    if (newPassword.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    } else {
      const errors = confirmPassword.errors;
      if (errors) {
        delete errors['passwordMismatch'];
        if (Object.keys(errors).length === 0) {
          confirmPassword.setErrors(null);
        }
      }
      return null;
    }
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      return;
    }

    this.saving = true;
    this.errorMessage = '';

    const changePasswordData: ChangePasswordRequest = this.passwordForm.value;

    this.authService.changePassword(changePasswordData).subscribe({
      next: () => {
        this.dialogRef.close(true);
      },
      error: (error) => {
        console.error('Error changing password:', error);
        this.errorMessage = error.error?.message || 'Failed to change password. Please try again.';
        this.saving = false;
      }
    });
  }
}
