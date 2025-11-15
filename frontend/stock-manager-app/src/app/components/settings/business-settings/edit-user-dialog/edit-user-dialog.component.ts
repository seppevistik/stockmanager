import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { UpdateUserRequest, UserDto } from '../../../../models/user-management.model';
import { UserRole } from '../../../../models/user.model';

@Component({
  selector: 'app-edit-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatIconModule
  ],
  templateUrl: './edit-user-dialog.component.html',
  styleUrls: ['./edit-user-dialog.component.scss']
})
export class EditUserDialogComponent {
  userForm: FormGroup;

  roles = [
    { value: UserRole.Admin, label: 'Admin' },
    { value: UserRole.Manager, label: 'Manager' },
    { value: UserRole.Staff, label: 'Staff' },
    { value: UserRole.Viewer, label: 'Viewer' }
  ];

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<EditUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { user: UserDto }
  ) {
    this.userForm = this.fb.group({
      email: [data.user.email, [Validators.required, Validators.email]],
      firstName: [data.user.firstName, Validators.required],
      lastName: [data.user.lastName, Validators.required],
      role: [data.user.role, Validators.required]
    });
  }

  onSubmit(): void {
    if (this.userForm.valid) {
      const userData: UpdateUserRequest = this.userForm.value;
      this.dialogRef.close(userData);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
