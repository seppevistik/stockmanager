import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SalesOrderService } from '../../../services/sales-order.service';
import {
  SalesOrder,
  SalesOrderStatus,
  SalesOrderLineStatus,
  Priority
} from '../../../models/sales-order.model';
import { ShippingDialogComponent } from '../dialogs/shipping-dialog/shipping-dialog.component';
import { ReasonDialogComponent } from '../dialogs/reason-dialog/reason-dialog.component';
import { DeliveryDialogComponent } from '../dialogs/delivery-dialog/delivery-dialog.component';
import { PickingDialogComponent } from '../dialogs/picking-dialog/picking-dialog.component';
import { ConfirmationDialogComponent } from '../dialogs/confirmation-dialog/confirmation-dialog.component';
import { SalesOrderStatusStepperComponent } from './sales-order-status-stepper/sales-order-status-stepper.component';

@Component({
  selector: 'app-sales-order-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule,
    MatMenuModule,
    MatDividerModule,
    MatTooltipModule,
    SalesOrderStatusStepperComponent
  ],
  templateUrl: './sales-order-detail.component.html',
  styleUrl: './sales-order-detail.component.scss'
})
export class SalesOrderDetailComponent implements OnInit {
  salesOrder?: SalesOrder;
  loading = false;
  actionInProgress = false;

  displayedColumns: string[] = [
    'productSku',
    'productName',
    'quantityOrdered',
    'quantityPicked',
    'quantityShipped',
    'quantityOutstanding',
    'unitPrice',
    'lineTotal',
    'status'
  ];

  SalesOrderStatus = SalesOrderStatus;

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private salesOrderService: SalesOrderService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadSalesOrder(+id);
    }
  }

  loadSalesOrder(id: number): void {
    this.loading = true;
    this.salesOrderService.getSalesOrder(id).subscribe({
      next: (order) => {
        this.salesOrder = order;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading sales order:', error);
        this.snackBar.open('Error loading sales order', 'Close', { duration: 3000 });
        this.loading = false;
        this.router.navigate(['/sales-orders']);
      }
    });
  }

  // Workflow Actions
  onSubmit(): void {
    if (!this.salesOrder || !this.canSubmit()) return;

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Submit Order',
        message: 'Submit this order for review? Once submitted, it can be confirmed for fulfillment.',
        confirmText: 'Submit',
        icon: 'send'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;

      this.actionInProgress = true;
      this.salesOrderService.submitOrder(this.salesOrder!.id).subscribe({
        next: () => {
          this.snackBar.open('Order submitted successfully', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error submitting order:', error);
          this.snackBar.open(error.error?.message || 'Error submitting order', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onConfirm(): void {
    if (!this.salesOrder || !this.canConfirm()) return;

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Confirm Order',
        message: 'Confirm this order? This will make it ready for fulfillment and start the picking process.',
        confirmText: 'Confirm',
        icon: 'check_circle'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;

      this.actionInProgress = true;
      this.salesOrderService.confirmOrder(this.salesOrder!.id).subscribe({
        next: () => {
          this.snackBar.open('Order confirmed successfully', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error confirming order:', error);
          this.snackBar.open(error.error?.message || 'Error confirming order', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onStartPicking(): void {
    if (!this.salesOrder || !this.canStartPicking()) return;

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Start Picking',
        message: 'Start picking this order? The order status will change to "Picking".',
        confirmText: 'Start Picking',
        icon: 'playlist_add_check'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;

      this.actionInProgress = true;
      this.salesOrderService.startPicking(this.salesOrder!.id).subscribe({
        next: () => {
          this.snackBar.open('Picking started', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error starting picking:', error);
          this.snackBar.open(error.error?.message || 'Error starting picking', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onCompletePicking(): void {
    if (!this.salesOrder || !this.canCompletePicking()) return;

    const dialogRef = this.dialog.open(PickingDialogComponent, {
      width: '700px',
      data: {
        lines: this.salesOrder.lines
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;

      this.actionInProgress = true;
      this.salesOrderService.completePicking(this.salesOrder!.id, result).subscribe({
        next: () => {
          this.snackBar.open('Picking completed', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error completing picking:', error);
          this.snackBar.open(error.error?.message || 'Error completing picking', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onStartPacking(): void {
    if (!this.salesOrder || !this.canStartPacking()) return;

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Start Packing',
        message: 'Start packing this order? The order status will change to "Packing".',
        confirmText: 'Start Packing',
        icon: 'inventory_2'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;

      this.actionInProgress = true;
      this.salesOrderService.startPacking(this.salesOrder!.id).subscribe({
        next: () => {
          this.snackBar.open('Packing started', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error starting packing:', error);
          this.snackBar.open(error.error?.message || 'Error starting packing', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onCompletePacking(): void {
    if (!this.salesOrder || !this.canCompletePacking()) return;

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Complete Packing',
        message: 'Mark packing as complete? This order will be ready for shipping.',
        confirmText: 'Complete Packing',
        icon: 'check_box'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;

      this.actionInProgress = true;
      this.salesOrderService.completePacking(this.salesOrder!.id).subscribe({
        next: () => {
          this.snackBar.open('Packing completed', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error completing packing:', error);
          this.snackBar.open(error.error?.message || 'Error completing packing', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onShip(): void {
    if (!this.salesOrder || !this.canShip()) return;

    const dialogRef = this.dialog.open(ShippingDialogComponent, {
      width: '500px',
      data: {
        carrier: 'UPS',
        shippedDate: new Date()
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;

      this.actionInProgress = true;
      this.salesOrderService.shipOrder(this.salesOrder!.id, {
        shippedDate: result.shippedDate,
        carrier: result.carrier,
        trackingNumber: result.trackingNumber || undefined
      }).subscribe({
        next: () => {
          this.snackBar.open('Order shipped successfully', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error shipping order:', error);
          this.snackBar.open(error.error?.message || 'Error shipping order', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onDeliver(): void {
    if (!this.salesOrder || !this.canDeliver()) return;

    const dialogRef = this.dialog.open(DeliveryDialogComponent, {
      width: '500px',
      data: {
        deliveredDate: new Date()
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;

      this.actionInProgress = true;
      this.salesOrderService.deliverOrder(this.salesOrder!.id, {
        deliveredDate: result.deliveredDate,
        receivedBy: result.receivedBy || undefined
      }).subscribe({
        next: () => {
          this.snackBar.open('Order marked as delivered', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error marking as delivered:', error);
          this.snackBar.open(error.error?.message || 'Error marking as delivered', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onCancel(): void {
    if (!this.salesOrder || !this.canCancel()) return;

    const dialogRef = this.dialog.open(ReasonDialogComponent, {
      width: '500px',
      data: {
        title: 'Cancel Order',
        message: 'Please provide a reason for cancelling this order.',
        reasonLabel: 'Cancellation Reason',
        reasonOptions: [
          { value: 'Customer Request', label: 'Customer Request' },
          { value: 'Out of Stock', label: 'Out of Stock' },
          { value: 'Payment Issue', label: 'Payment Issue' },
          { value: 'Duplicate Order', label: 'Duplicate Order' },
          { value: 'Incorrect Information', label: 'Incorrect Information' }
        ],
        allowCustom: true
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;

      this.actionInProgress = true;
      this.salesOrderService.cancelOrder(this.salesOrder!.id, { reason: result.reason }).subscribe({
        next: () => {
          this.snackBar.open('Order cancelled', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error cancelling order:', error);
          this.snackBar.open(error.error?.message || 'Error cancelling order', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onHold(): void {
    if (!this.salesOrder || !this.canHold()) return;

    const dialogRef = this.dialog.open(ReasonDialogComponent, {
      width: '500px',
      data: {
        title: 'Place Order on Hold',
        message: 'Please provide a reason for placing this order on hold.',
        reasonLabel: 'Hold Reason',
        reasonOptions: [
          { value: 'Awaiting Payment', label: 'Awaiting Payment' },
          { value: 'Customer Request', label: 'Customer Request' },
          { value: 'Inventory Issue', label: 'Inventory Issue' },
          { value: 'Address Verification', label: 'Address Verification' },
          { value: 'Quality Check', label: 'Quality Check' }
        ],
        allowCustom: true
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;

      this.actionInProgress = true;
      this.salesOrderService.holdOrder(this.salesOrder!.id, { reason: result.reason }).subscribe({
        next: () => {
          this.snackBar.open('Order placed on hold', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error placing order on hold:', error);
          this.snackBar.open(error.error?.message || 'Error placing order on hold', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onRelease(): void {
    if (!this.salesOrder || !this.canRelease()) return;

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Release Order from Hold',
        message: 'Release this order from hold? The order will return to its previous status.',
        confirmText: 'Release',
        icon: 'play_arrow'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;

      this.actionInProgress = true;
      this.salesOrderService.releaseOrder(this.salesOrder!.id).subscribe({
        next: () => {
          this.snackBar.open('Order released from hold', 'Close', { duration: 3000 });
          this.loadSalesOrder(this.salesOrder!.id);
          this.actionInProgress = false;
        },
        error: (error) => {
          console.error('Error releasing order:', error);
          this.snackBar.open(error.error?.message || 'Error releasing order', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  onEdit(): void {
    if (this.salesOrder && this.canEdit()) {
      this.router.navigate(['/sales-orders/edit', this.salesOrder.id]);
    }
  }

  onDelete(): void {
    if (!this.salesOrder || !this.canDelete()) return;

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Order',
        message: `Are you sure you want to delete order ${this.salesOrder.orderNumber}? This action cannot be undone.`,
        confirmText: 'Delete',
        confirmColor: 'warn',
        icon: 'delete'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;

      this.actionInProgress = true;
      this.salesOrderService.deleteSalesOrder(this.salesOrder!.id).subscribe({
        next: () => {
          this.snackBar.open('Order deleted', 'Close', { duration: 3000 });
          this.router.navigate(['/sales-orders']);
        },
        error: (error) => {
          console.error('Error deleting order:', error);
          this.snackBar.open(error.error?.message || 'Error deleting order', 'Close', { duration: 3000 });
          this.actionInProgress = false;
        }
      });
    });
  }

  // Permission checks
  canEdit(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.Draft;
  }

  canDelete(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.Draft;
  }

  canSubmit(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.Draft;
  }

  canConfirm(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.Submitted;
  }

  canStartPicking(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.Confirmed ||
           this.salesOrder?.status === SalesOrderStatus.AwaitingPickup;
  }

  canCompletePicking(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.Picking;
  }

  canStartPacking(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.Picked;
  }

  canCompletePacking(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.Packing;
  }

  canShip(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.Packed;
  }

  canDeliver(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.Shipped;
  }

  canCancel(): boolean {
    return this.salesOrder?.status !== SalesOrderStatus.Shipped &&
           this.salesOrder?.status !== SalesOrderStatus.Delivered &&
           this.salesOrder?.status !== SalesOrderStatus.Cancelled;
  }

  canHold(): boolean {
    return this.salesOrder?.status !== SalesOrderStatus.Cancelled &&
           this.salesOrder?.status !== SalesOrderStatus.Shipped &&
           this.salesOrder?.status !== SalesOrderStatus.Delivered &&
           this.salesOrder?.status !== SalesOrderStatus.OnHold;
  }

  canRelease(): boolean {
    return this.salesOrder?.status === SalesOrderStatus.OnHold;
  }

  // Display helpers
  getStatusColor(status: SalesOrderStatus): string {
    const colors: { [key in SalesOrderStatus]: string } = {
      [SalesOrderStatus.Draft]: '',
      [SalesOrderStatus.Submitted]: 'accent',
      [SalesOrderStatus.Confirmed]: 'primary',
      [SalesOrderStatus.AwaitingPickup]: 'accent',
      [SalesOrderStatus.Picking]: 'accent',
      [SalesOrderStatus.Picked]: 'primary',
      [SalesOrderStatus.Packing]: 'accent',
      [SalesOrderStatus.Packed]: 'primary',
      [SalesOrderStatus.Shipped]: 'primary',
      [SalesOrderStatus.Delivered]: 'primary',
      [SalesOrderStatus.Cancelled]: 'warn',
      [SalesOrderStatus.OnHold]: 'warn'
    };
    return colors[status] || '';
  }

  getPriorityColor(priority: Priority): string {
    const colors: { [key in Priority]: string } = {
      [Priority.Low]: '',
      [Priority.Normal]: '',
      [Priority.High]: 'accent',
      [Priority.Urgent]: 'warn'
    };
    return colors[priority] || '';
  }

  getLineStatusColor(status: SalesOrderLineStatus): string {
    const colors: { [key in SalesOrderLineStatus]: string } = {
      [SalesOrderLineStatus.Pending]: 'primary',
      [SalesOrderLineStatus.Allocated]: 'accent',
      [SalesOrderLineStatus.Picked]: 'accent',
      [SalesOrderLineStatus.Packed]: 'primary',
      [SalesOrderLineStatus.Shipped]: 'primary',
      [SalesOrderLineStatus.Cancelled]: 'warn'
    };
    return colors[status] || '';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  formatDate(date: Date | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString();
  }

  formatDateTime(date: Date | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString();
  }
}
