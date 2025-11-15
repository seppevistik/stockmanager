import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { CreateUserRequest } from '../../../../models/user-management.model';
import { UserRole } from '../../../../models/user.model';

@Component({
  selector: 'app-create-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatCheckboxModule,
    MatIconModule
  ],
  templateUrl: './create-user-dialog.component.html',
  styleUrls: ['./create-user-dialog.component.scss']
})
export class CreateUserDialogComponent {
  userForm: FormGroup;
  hidePassword = true;

  roles = [
    { value: UserRole.Admin, label: 'Admin' },
    { value: UserRole.Manager, label: 'Manager' },
    { value: UserRole.Staff, label: 'Staff' },
    { value: UserRole.Viewer, label: 'Viewer' }
  ];

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<CreateUserDialogComponent>
  ) {
    this.userForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      role: [UserRole.Staff, Validators.required],
      temporaryPassword: ['', [Validators.required, Validators.minLength(6)]],
      sendWelcomeEmail: [true]
    });
  }

  onSubmit(): void {
    if (this.userForm.valid) {
      const userData: CreateUserRequest = this.userForm.value;
      this.dialogRef.close(userData);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
