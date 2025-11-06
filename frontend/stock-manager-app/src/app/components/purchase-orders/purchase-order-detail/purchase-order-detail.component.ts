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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { PurchaseOrderService } from '../../../services/purchase-order.service';
import { ReceiptService } from '../../../services/receipt.service';
import { PurchaseOrder, PurchaseOrderStatus } from '../../../models/purchase-order.model';
import { Receipt } from '../../../models/receipt.model';

@Component({
  selector: 'app-purchase-order-detail',
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
    MatDatepickerModule,
    MatNativeDateModule
  ],
  templateUrl: './purchase-order-detail.component.html',
  styleUrl: './purchase-order-detail.component.scss'
})
export class PurchaseOrderDetailComponent implements OnInit {
  purchaseOrder?: PurchaseOrder;
  receipts: Receipt[] = [];
  loading = false;
  actionInProgress = false;
  errorMessage = '';

  displayedColumns: string[] = ['productSku', 'productName', 'quantityOrdered', 'quantityReceived', 'quantityOutstanding', 'unitPrice', 'lineTotal', 'status'];
  receiptsColumns: string[] = ['receiptNumber', 'receiptDate', 'receivedByName', 'status', 'actions'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private purchaseOrderService: PurchaseOrderService,
    private receiptService: ReceiptService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadPurchaseOrder(+id);
      this.loadReceipts(+id);
    }
  }

  loadPurchaseOrder(id: number): void {
    this.loading = true;
    this.purchaseOrderService.getById(id).subscribe({
      next: (po) => {
        this.purchaseOrder = po;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading purchase order:', error);
        this.snackBar.open('Error loading purchase order', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  loadReceipts(purchaseOrderId: number): void {
    this.receiptService.getByPurchaseOrder(purchaseOrderId).subscribe({
      next: (receipts) => {
        this.receipts = receipts;
      },
      error: (error) => {
        console.error('Error loading receipts:', error);
      }
    });
  }

  getStatusColor(status: PurchaseOrderStatus): string {
    const colors: { [key: string]: string } = {
      'Draft': 'primary',
      'Submitted': 'accent',
      'Confirmed': 'accent',
      'Receiving': 'warn',
      'PartiallyReceived': 'warn',
      'Completed': '',
      'Cancelled': ''
    };
    return colors[status] || '';
  }

  getLineStatusColor(status: string): string {
    const colors: { [key: string]: string } = {
      'Pending': 'primary',
      'PartiallyReceived': 'accent',
      'FullyReceived': '',
      'Cancelled': '',
      'ShortShipped': 'warn'
    };
    return colors[status] || '';
  }

  canEdit(): boolean {
    return this.purchaseOrder?.status === PurchaseOrderStatus.Draft;
  }

  canSubmit(): boolean {
    return this.purchaseOrder?.status === PurchaseOrderStatus.Draft;
  }

  canConfirm(): boolean {
    return this.purchaseOrder?.status === PurchaseOrderStatus.Submitted;
  }

  canCancel(): boolean {
    return this.purchaseOrder?.status !== PurchaseOrderStatus.Completed &&
           this.purchaseOrder?.status !== PurchaseOrderStatus.Cancelled;
  }

  canCreateReceipt(): boolean {
    return this.purchaseOrder?.status === PurchaseOrderStatus.Confirmed ||
           this.purchaseOrder?.status === PurchaseOrderStatus.Receiving ||
           this.purchaseOrder?.status === PurchaseOrderStatus.PartiallyReceived;
  }

  canDelete(): boolean {
    return this.purchaseOrder?.status === PurchaseOrderStatus.Draft;
  }

  onEdit(): void {
    if (this.purchaseOrder) {
      this.router.navigate(['/purchase-orders', 'edit', this.purchaseOrder.id]);
    }
  }

  onSubmit(): void {
    if (!this.purchaseOrder) return;

    this.actionInProgress = true;
    this.purchaseOrderService.submit(this.purchaseOrder.id).subscribe({
      next: () => {
        this.snackBar.open('Purchase order submitted successfully', 'Close', { duration: 3000 });
        this.loadPurchaseOrder(this.purchaseOrder!.id);
        this.actionInProgress = false;
      },
      error: (error) => {
        console.error('Error submitting purchase order:', error);
        this.snackBar.open('Error submitting purchase order', 'Close', { duration: 3000 });
        this.actionInProgress = false;
      }
    });
  }

  onConfirm(): void {
    if (!this.purchaseOrder) return;

    const dialogRef = this.dialog.open(ConfirmDeliveryDialog, {
      width: '400px',
      data: { expectedDate: this.purchaseOrder.expectedDeliveryDate }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result && this.purchaseOrder) {
        this.actionInProgress = true;
        this.purchaseOrderService.confirm(this.purchaseOrder.id, result).subscribe({
          next: () => {
            this.snackBar.open('Purchase order confirmed successfully', 'Close', { duration: 3000 });
            this.loadPurchaseOrder(this.purchaseOrder!.id);
            this.actionInProgress = false;
          },
          error: (error) => {
            console.error('Error confirming purchase order:', error);
            this.snackBar.open('Error confirming purchase order', 'Close', { duration: 3000 });
            this.actionInProgress = false;
          }
        });
      }
    });
  }

  onCancel(): void {
    if (!this.purchaseOrder) return;

    const dialogRef = this.dialog.open(CancelPurchaseOrderDialog, {
      width: '400px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result && this.purchaseOrder) {
        this.actionInProgress = true;
        this.purchaseOrderService.cancel(this.purchaseOrder.id, result).subscribe({
          next: () => {
            this.snackBar.open('Purchase order cancelled', 'Close', { duration: 3000 });
            this.loadPurchaseOrder(this.purchaseOrder!.id);
            this.actionInProgress = false;
          },
          error: (error) => {
            console.error('Error cancelling purchase order:', error);
            this.snackBar.open('Error cancelling purchase order', 'Close', { duration: 3000 });
            this.actionInProgress = false;
          }
        });
      }
    });
  }

  onDelete(): void {
    if (!this.purchaseOrder) return;

    if (confirm('Are you sure you want to delete this purchase order?')) {
      this.actionInProgress = true;
      this.purchaseOrderService.delete(this.purchaseOrder.id).subscribe({
        next: () => {
          this.snackBar.open('Purchase order deleted', 'Close', { duration: 3000 });
          this.router.navigate(['/purchase-orders']);
        },
        error: (error) => {
          console.error('Error deleting purchase order:', error);
          this.snackBar.open('Error deleting purchase order', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    }
  }

  onCreateReceipt(): void {
    if (this.purchaseOrder) {
      this.router.navigate(['/receipts', 'new'], {
        queryParams: { poId: this.purchaseOrder.id }
      });
    }
  }

  viewReceipt(receipt: Receipt): void {
    this.router.navigate(['/receipts', receipt.id]);
  }

  getReceiptStatusColor(status: string): string {
    const colors: { [key: string]: string } = {
      'Draft': 'primary',
      'Validated': '',
      'PendingValidation': 'warn',
      'Approved': '',
      'Rejected': '',
      'Completed': ''
    };
    return colors[status] || '';
  }

  getReceiptStatusLabel(status: string): string {
    const labels: { [key: string]: string } = {
      'InProgress': 'In Progress',
      'PendingValidation': 'Pending Validation',
      'Validated': 'Validated',
      'Completed': 'Completed',
      'Rejected': 'Rejected'
    };
    return labels[status] || status;
  }

  back(): void {
    this.router.navigate(['/purchase-orders']);
  }
}

// Confirm Delivery Dialog Component
@Component({
  selector: 'confirm-delivery-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule
  ],
  template: `
    <h2 mat-dialog-title>Confirm Delivery Date</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field class="full-width">
          <mat-label>Confirmed Delivery Date</mat-label>
          <input matInput [matDatepicker]="picker" formControlName="confirmedDeliveryDate" required>
          <mat-datepicker-toggle matSuffix [for]="picker"></mat-datepicker-toggle>
          <mat-datepicker #picker></mat-datepicker>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" [disabled]="form.invalid" (click)="onConfirm()">Confirm</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width {
      width: 100%;
    }
  `]
})
export class ConfirmDeliveryDialog {
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<ConfirmDeliveryDialog>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    const expectedDate = data?.expectedDate;
    this.form = this.fb.group({
      confirmedDeliveryDate: [expectedDate || new Date(), Validators.required]
    });
  }

  onConfirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}

// Cancel Purchase Order Dialog Component
@Component({
  selector: 'cancel-purchase-order-dialog',
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
    <h2 mat-dialog-title>Cancel Purchase Order</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field class="full-width">
          <mat-label>Cancellation Reason</mat-label>
          <textarea matInput formControlName="reason" rows="4" required></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="warn" [disabled]="form.invalid" (click)="onCancel()">Cancel Order</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width {
      width: 100%;
    }
  `]
})
export class CancelPurchaseOrderDialog {
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<CancelPurchaseOrderDialog>
  ) {
    this.form = this.fb.group({
      reason: ['', Validators.required]
    });
  }

  onCancel(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
