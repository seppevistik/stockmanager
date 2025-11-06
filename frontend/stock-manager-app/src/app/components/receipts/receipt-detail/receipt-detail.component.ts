import { Component, OnInit, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { ReceiptService } from '../../../services/receipt.service';
import { Receipt, ReceiptStatus, ItemCondition, ReceiptValidation } from '../../../models/receipt.model';

@Component({
  selector: 'app-receipt-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatExpansionModule,
    MatDividerModule
  ],
  templateUrl: './receipt-detail.component.html',
  styleUrl: './receipt-detail.component.scss'
})
export class ReceiptDetailComponent implements OnInit {
  receipt?: Receipt;
  validation?: ReceiptValidation;
  loading = false;
  actionInProgress = false;
  errorMessage = '';

  displayedColumns: string[] = ['productSku', 'productName', 'quantityOrdered', 'quantityReceived', 'quantityVariance', 'unitPriceOrdered', 'unitPriceReceived', 'priceVariance', 'condition'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private receiptService: ReceiptService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadReceipt(+id);
    }
  }

  loadReceipt(id: number): void {
    this.loading = true;
    this.receiptService.getById(id).subscribe({
      next: (receipt) => {
        this.receipt = receipt;
        this.loading = false;

        // If pending validation, load validation details
        if (receipt.status === ReceiptStatus.PendingValidation) {
          this.loadValidation(id);
        }
      },
      error: (error) => {
        console.error('Error loading receipt:', error);
        this.snackBar.open('Error loading receipt', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  loadValidation(receiptId: number): void {
    this.receiptService.validate(receiptId).subscribe({
      next: (validation) => {
        this.validation = validation;
      },
      error: (error) => {
        console.error('Error loading validation:', error);
      }
    });
  }

  getStatusColor(status: ReceiptStatus): string {
    const colors: { [key: string]: string } = {
      'InProgress': 'primary',
      'PendingValidation': 'warn',
      'Validated': 'accent',
      'Completed': '',
      'Rejected': ''
    };
    return colors[status] || '';
  }

  getStatusLabel(status: ReceiptStatus): string {
    const labels: { [key: string]: string } = {
      'InProgress': 'In Progress',
      'PendingValidation': 'Pending Validation',
      'Validated': 'Validated',
      'Completed': 'Completed',
      'Rejected': 'Rejected'
    };
    return labels[status] || status.toString();
  }

  getConditionColor(condition: ItemCondition): string {
    const colors: { [key: string]: string } = {
      'Good': '',
      'Damaged': 'warn',
      'Defective': ''
    };
    return colors[condition] || '';
  }

  canApprove(): boolean {
    return this.receipt?.status === ReceiptStatus.PendingValidation;
  }

  canReject(): boolean {
    return this.receipt?.status === ReceiptStatus.PendingValidation;
  }

  canComplete(): boolean {
    return this.receipt?.status === ReceiptStatus.Validated;
  }

  canDelete(): boolean {
    return this.receipt?.status !== ReceiptStatus.Completed;
  }

  onApprove(): void {
    if (!this.receipt) return;

    const dialogRef = this.dialog.open(ApproveReceiptDialog, {
      width: '500px',
      data: { hasVariances: this.receipt.hasVariances }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result !== undefined && this.receipt) {
        this.actionInProgress = true;
        this.receiptService.approve(this.receipt.id, result).subscribe({
          next: () => {
            this.snackBar.open('Receipt approved successfully', 'Close', { duration: 3000 });
            this.loadReceipt(this.receipt!.id);
            this.actionInProgress = false;
          },
          error: (error) => {
            console.error('Error approving receipt:', error);
            this.snackBar.open('Error approving receipt', 'Close', { duration: 3000 });
            this.actionInProgress = false;
          }
        });
      }
    });
  }

  onReject(): void {
    if (!this.receipt) return;

    const dialogRef = this.dialog.open(RejectReceiptDialog, {
      width: '400px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result && this.receipt) {
        this.actionInProgress = true;
        this.receiptService.reject(this.receipt.id, result).subscribe({
          next: () => {
            this.snackBar.open('Receipt rejected', 'Close', { duration: 3000 });
            this.loadReceipt(this.receipt!.id);
            this.actionInProgress = false;
          },
          error: (error) => {
            console.error('Error rejecting receipt:', error);
            this.snackBar.open('Error rejecting receipt', 'Close', { duration: 3000 });
            this.actionInProgress = false;
          }
        });
      }
    });
  }

  onComplete(): void {
    if (!this.receipt) return;

    if (confirm('Complete this receipt and update inventory? This action cannot be undone.')) {
      this.actionInProgress = true;
      this.receiptService.complete(this.receipt.id).subscribe({
        next: () => {
          this.snackBar.open('Receipt completed and inventory updated', 'Close', { duration: 3000 });
          this.loadReceipt(this.receipt!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error completing receipt:', error);
          this.snackBar.open('Error completing receipt', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    }
  }

  onDelete(): void {
    if (!this.receipt) return;

    if (confirm(`Are you sure you want to delete receipt ${this.receipt.receiptNumber}?`)) {
      this.actionInProgress = true;
      this.receiptService.delete(this.receipt.id).subscribe({
        next: () => {
          this.snackBar.open('Receipt deleted', 'Close', { duration: 3000 });
          this.router.navigate(['/receipts']);
        },
        error: (error) => {
          console.error('Error deleting receipt:', error);
          this.snackBar.open('Error deleting receipt', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    }
  }

  viewPurchaseOrder(): void {
    if (this.receipt) {
      this.router.navigate(['/purchase-orders', this.receipt.purchaseOrderId]);
    }
  }

  back(): void {
    this.router.navigate(['/receipts']);
  }
}

// Approve Receipt Dialog Component
@Component({
  selector: 'approve-receipt-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ],
  template: `
    <h2 mat-dialog-title>Approve Receipt</h2>
    <mat-dialog-content>
      <p *ngIf="hasVariances" class="warning-text">
        <strong>Note:</strong> This receipt has variances. Please provide notes explaining the variances.
      </p>
      <form [formGroup]="form">
        <mat-form-field class="full-width">
          <mat-label>Variance Notes {{ hasVariances ? '(Required)' : '(Optional)' }}</mat-label>
          <textarea matInput formControlName="varianceNotes" rows="4"
                    placeholder="Explain any quantity or price differences..."></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" [disabled]="form.invalid" (click)="onApprove()">Approve</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width {
      width: 100%;
    }
    .warning-text {
      background-color: #fff3cd;
      border: 1px solid #ffc107;
      padding: 12px;
      border-radius: 4px;
      margin-bottom: 16px;
    }
  `]
})
export class ApproveReceiptDialog {
  form: FormGroup;
  hasVariances: boolean;

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<ApproveReceiptDialog>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.hasVariances = data?.hasVariances || false;

    this.form = this.fb.group({
      varianceNotes: [
        '',
        this.hasVariances ? [Validators.required] : []
      ]
    });
  }

  onApprove(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}

// Reject Receipt Dialog Component
@Component({
  selector: 'reject-receipt-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ],
  template: `
    <h2 mat-dialog-title>Reject Receipt</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field class="full-width">
          <mat-label>Rejection Reason</mat-label>
          <textarea matInput formControlName="reason" rows="4" required
                    placeholder="Explain why this receipt is being rejected..."></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="warn" [disabled]="form.invalid" (click)="onReject()">Reject</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width {
      width: 100%;
    }
  `]
})
export class RejectReceiptDialog {
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<RejectReceiptDialog>
  ) {
    this.form = this.fb.group({
      reason: ['', Validators.required]
    });
  }

  onReject(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
