import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';

export interface ReasonDialogData {
  title: string;
  message: string;
  reasonLabel?: string;
  reasonOptions?: { value: string; label: string }[];
  allowCustom?: boolean;
}

@Component({
  selector: 'app-reason-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" style="width: 100%; margin-bottom: 16px;" *ngIf="data.reasonOptions && data.reasonOptions.length > 0">
          <mat-label>{{ data.reasonLabel || 'Reason' }}</mat-label>
          <mat-select formControlName="reason" required (selectionChange)="onReasonChange($event.value)">
            <mat-option *ngFor="let option of data.reasonOptions" [value]="option.value">
              {{ option.label }}
            </mat-option>
            <mat-option value="other" *ngIf="data.allowCustom">Other (specify below)</mat-option>
          </mat-select>
          <mat-error *ngIf="form.get('reason')?.hasError('required')">
            Reason is required
          </mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline" style="width: 100%;" *ngIf="showCustomReason">
          <mat-label>{{ data.reasonLabel || 'Reason' }}</mat-label>
          <textarea matInput formControlName="customReason" rows="3"
                    placeholder="Enter reason..." required></textarea>
          <mat-error *ngIf="form.get('customReason')?.hasError('required')">
            Please provide a reason
          </mat-error>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancel</button>
      <button mat-raised-button color="primary" (click)="onConfirm()" [disabled]="form.invalid">
        Confirm
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content {
      min-width: 400px;
      padding: 20px 24px;
    }

    p {
      margin-bottom: 20px;
      color: rgba(0, 0, 0, 0.6);
    }
  `]
})
export class ReasonDialogComponent {
  form: FormGroup;
  showCustomReason = false;

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<ReasonDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ReasonDialogData
  ) {
    const hasOptions = data.reasonOptions && data.reasonOptions.length > 0;
    this.showCustomReason = !hasOptions;

    this.form = this.fb.group({
      reason: [hasOptions ? '' : null, hasOptions ? Validators.required : null],
      customReason: ['', this.showCustomReason ? Validators.required : null]
    });
  }

  onReasonChange(value: string): void {
    this.showCustomReason = value === 'other';
    const customReasonControl = this.form.get('customReason');

    if (this.showCustomReason) {
      customReasonControl?.setValidators(Validators.required);
      customReasonControl?.setValue('');
    } else {
      customReasonControl?.clearValidators();
      customReasonControl?.setValue('');
    }
    customReasonControl?.updateValueAndValidity();
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onConfirm(): void {
    if (this.form.valid) {
      const reason = this.showCustomReason
        ? this.form.value.customReason
        : this.form.value.reason;
      this.dialogRef.close({ reason });
    }
  }
}
