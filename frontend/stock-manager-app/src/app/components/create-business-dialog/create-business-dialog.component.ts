import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { BusinessService } from '../../services/business.service';
import { UserRole } from '../../models/user.model';

@Component({
  selector: 'app-create-business-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  templateUrl: './create-business-dialog.component.html',
  styleUrl: './create-business-dialog.component.scss'
})
export class CreateBusinessDialogComponent {
  businessForm: FormGroup;
  loading = false;
  errorMessage = '';

  roles = [
    { value: UserRole.Admin, label: 'Admin' },
    { value: UserRole.Manager, label: 'Manager' },
    { value: UserRole.Staff, label: 'Staff' },
    { value: UserRole.Viewer, label: 'Viewer' }
  ];

  constructor(
    private fb: FormBuilder,
    private businessService: BusinessService,
    private dialogRef: MatDialogRef<CreateBusinessDialogComponent>
  ) {
    this.businessForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      userRole: [UserRole.Admin, Validators.required]
    });
  }

  onSubmit(): void {
    if (this.businessForm.invalid) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.businessService.createBusiness(this.businessForm.value).subscribe({
      next: (response) => {
        this.dialogRef.close(response);
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = error.error?.message || 'Failed to create business. Please try again.';
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  get name() {
    return this.businessForm.get('name');
  }

  get description() {
    return this.businessForm.get('description');
  }
}
