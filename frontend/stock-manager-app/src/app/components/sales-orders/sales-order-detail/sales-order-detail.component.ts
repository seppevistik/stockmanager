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
    MatTooltipModule
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

    const confirmed = confirm('Submit this order for review?');
    if (!confirmed) return;

    this.actionInProgress = true;
    this.salesOrderService.submitOrder(this.salesOrder.id).subscribe({
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
  }

  onConfirm(): void {
    if (!this.salesOrder || !this.canConfirm()) return;

    const confirmed = confirm('Confirm this order? This will make it ready for fulfillment.');
    if (!confirmed) return;

    this.actionInProgress = true;
    this.salesOrderService.confirmOrder(this.salesOrder.id).subscribe({
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
  }

  onStartPicking(): void {
    if (!this.salesOrder || !this.canStartPicking()) return;

    const confirmed = confirm('Start picking this order?');
    if (!confirmed) return;

    this.actionInProgress = true;
    this.salesOrderService.startPicking(this.salesOrder.id).subscribe({
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
  }

  onCompletePicking(): void {
    if (!this.salesOrder || !this.canCompletePicking()) return;

    // Build picked lines from order lines
    const pickedLines = this.salesOrder.lines.map(line => ({
      lineId: line.id,
      quantityPicked: line.quantityOrdered, // Default to full quantity
      location: undefined,
      notes: undefined
    }));

    const confirmed = confirm('Mark all items as picked with ordered quantities?');
    if (!confirmed) return;

    this.actionInProgress = true;
    this.salesOrderService.completePicking(this.salesOrder.id, { pickedLines }).subscribe({
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
  }

  onStartPacking(): void {
    if (!this.salesOrder || !this.canStartPacking()) return;

    const confirmed = confirm('Start packing this order?');
    if (!confirmed) return;

    this.actionInProgress = true;
    this.salesOrderService.startPacking(this.salesOrder.id).subscribe({
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
  }

  onCompletePacking(): void {
    if (!this.salesOrder || !this.canCompletePacking()) return;

    const confirmed = confirm('Mark packing as complete?');
    if (!confirmed) return;

    this.actionInProgress = true;
    this.salesOrderService.completePacking(this.salesOrder.id).subscribe({
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
  }

  onShip(): void {
    if (!this.salesOrder || !this.canShip()) return;

    const carrier = prompt('Enter carrier name:', 'UPS');
    if (!carrier) return;

    const trackingNumber = prompt('Enter tracking number (optional):');

    this.actionInProgress = true;
    this.salesOrderService.shipOrder(this.salesOrder.id, {
      shippedDate: new Date(),
      carrier,
      trackingNumber: trackingNumber || undefined
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
  }

  onDeliver(): void {
    if (!this.salesOrder || !this.canDeliver()) return;

    const receivedBy = prompt('Received by (optional):');

    const confirmed = confirm('Mark this order as delivered?');
    if (!confirmed) return;

    this.actionInProgress = true;
    this.salesOrderService.deliverOrder(this.salesOrder.id, {
      deliveredDate: new Date(),
      receivedBy: receivedBy || undefined
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
  }

  onCancel(): void {
    if (!this.salesOrder || !this.canCancel()) return;

    const reason = prompt('Enter cancellation reason:');
    if (!reason) return;

    this.actionInProgress = true;
    this.salesOrderService.cancelOrder(this.salesOrder.id, { reason }).subscribe({
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
  }

  onHold(): void {
    if (!this.salesOrder || !this.canHold()) return;

    const reason = prompt('Enter reason for hold:');
    if (!reason) return;

    this.actionInProgress = true;
    this.salesOrderService.holdOrder(this.salesOrder.id, { reason }).subscribe({
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
  }

  onRelease(): void {
    if (!this.salesOrder || !this.canRelease()) return;

    const confirmed = confirm('Release this order from hold?');
    if (!confirmed) return;

    this.actionInProgress = true;
    this.salesOrderService.releaseOrder(this.salesOrder.id).subscribe({
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
  }

  onEdit(): void {
    if (this.salesOrder && this.canEdit()) {
      this.router.navigate(['/sales-orders/edit', this.salesOrder.id]);
    }
  }

  onDelete(): void {
    if (!this.salesOrder || !this.canDelete()) return;

    const confirmed = confirm(`Are you sure you want to delete order ${this.salesOrder.orderNumber}?`);
    if (!confirmed) return;

    this.actionInProgress = true;
    this.salesOrderService.deleteSalesOrder(this.salesOrder.id).subscribe({
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
