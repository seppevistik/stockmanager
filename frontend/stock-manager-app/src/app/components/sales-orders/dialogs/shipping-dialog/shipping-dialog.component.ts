import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';

export interface ShippingDialogData {
  carrier?: string;
  trackingNumber?: string;
  shippedDate?: Date;
}

@Component({
  selector: 'app-shipping-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule
  ],
  template: `
    <h2 mat-dialog-title>Ship Order</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 16px;">
          <mat-label>Carrier</mat-label>
          <mat-select formControlName="carrier" required>
            <mat-option value="UPS">UPS</mat-option>
            <mat-option value="FedEx">FedEx</mat-option>
            <mat-option value="USPS">USPS</mat-option>
            <mat-option value="DHL">DHL</mat-option>
            <mat-option value="Other">Other</mat-option>
          </mat-select>
          <mat-error *ngIf="form.get('carrier')?.hasError('required')">
            Carrier is required
          </mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 16px;">
          <mat-label>Tracking Number</mat-label>
          <input matInput formControlName="trackingNumber" placeholder="Enter tracking number">
        </mat-form-field>

        <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 16px;">
          <mat-label>Shipped Date</mat-label>
          <input matInput [matDatepicker]="picker" formControlName="shippedDate" required>
          <mat-datepicker-toggle matSuffix [for]="picker"></mat-datepicker-toggle>
          <mat-datepicker #picker></mat-datepicker>
          <mat-error *ngIf="form.get('shippedDate')?.hasError('required')">
            Shipped date is required
          </mat-error>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancel</button>
      <button mat-raised-button color="primary" (click)="onConfirm()" [disabled]="form.invalid">
        Ship Order
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content {
      min-width: 400px;
      padding: 20px 24px;
    }
  `]
})
export class ShippingDialogComponent {
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<ShippingDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ShippingDialogData
  ) {
    this.form = this.fb.group({
      carrier: [data?.carrier || 'UPS', Validators.required],
      trackingNumber: [data?.trackingNumber || ''],
      shippedDate: [data?.shippedDate || new Date(), Validators.required]
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onConfirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
