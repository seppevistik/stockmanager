import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chip';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { SalesOrderService } from '../../../services/sales-order.service';
import {
  SalesOrderSummary,
  SalesOrderListQuery,
  SalesOrderStatistics,
  SalesOrderStatus,
  Priority
} from '../../../models/sales-order.model';

@Component({
  selector: 'app-sales-orders-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './sales-orders-list.component.html',
  styleUrl: './sales-orders-list.component.scss'
})
export class SalesOrdersListComponent implements OnInit {
  displayedColumns: string[] = [
    'orderNumber',
    'customer',
    'orderDate',
    'requiredDate',
    'status',
    'priority',
    'totalAmount',
    'actions'
  ];

  salesOrders: SalesOrderSummary[] = [];
  statistics?: SalesOrderStatistics;
  loading = false;

  searchControl = new FormControl('');
  statusFilter = new FormControl<SalesOrderStatus | null>(null);
  priorityFilter = new FormControl<Priority | null>(null);

  query: SalesOrderListQuery = {
    page: 1,
    pageSize: 10,
    sortBy: 'orderDate',
    sortDirection: 'desc'
  };

  totalCount = 0;
  pageSize = 10;
  pageIndex = 0;

  statuses = [
    { value: SalesOrderStatus.Draft, label: 'Draft' },
    { value: SalesOrderStatus.Submitted, label: 'Submitted' },
    { value: SalesOrderStatus.Confirmed, label: 'Confirmed' },
    { value: SalesOrderStatus.Picking, label: 'Picking' },
    { value: SalesOrderStatus.Picked, label: 'Picked' },
    { value: SalesOrderStatus.Packing, label: 'Packing' },
    { value: SalesOrderStatus.Packed, label: 'Packed' },
    { value: SalesOrderStatus.Shipped, label: 'Shipped' },
    { value: SalesOrderStatus.Delivered, label: 'Delivered' },
    { value: SalesOrderStatus.Cancelled, label: 'Cancelled' },
    { value: SalesOrderStatus.OnHold, label: 'On Hold' }
  ];

  priorities = [
    { value: Priority.Low, label: 'Low' },
    { value: Priority.Normal, label: 'Normal' },
    { value: Priority.High, label: 'High' },
    { value: Priority.Urgent, label: 'Urgent' }
  ];

  constructor(
    private salesOrderService: SalesOrderService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadSalesOrders();
    this.loadStatistics();
    this.setupFilters();
  }

  setupFilters(): void {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(value => {
        this.query.searchTerm = value || undefined;
        this.query.page = 1;
        this.pageIndex = 0;
        this.loadSalesOrders();
      });

    this.statusFilter.valueChanges.subscribe(value => {
      this.query.status = value !== null ? value : undefined;
      this.query.page = 1;
      this.pageIndex = 0;
      this.loadSalesOrders();
    });

    this.priorityFilter.valueChanges.subscribe(value => {
      this.query.priority = value !== null ? value : undefined;
      this.query.page = 1;
      this.pageIndex = 0;
      this.loadSalesOrders();
    });
  }

  loadSalesOrders(): void {
    this.loading = true;
    this.salesOrderService.getSalesOrders(this.query).subscribe({
      next: (result) => {
        this.salesOrders = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading sales orders:', error);
        this.snackBar.open('Error loading sales orders', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  loadStatistics(): void {
    this.salesOrderService.getStatistics().subscribe({
      next: (stats) => {
        this.statistics = stats;
      },
      error: (error) => {
        console.error('Error loading statistics:', error);
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.query.page = event.pageIndex + 1;
    this.query.pageSize = event.pageSize;
    this.pageSize = event.pageSize;
    this.pageIndex = event.pageIndex;
    this.loadSalesOrders();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusFilter.setValue(null);
    this.priorityFilter.setValue(null);
  }

  createSalesOrder(): void {
    this.router.navigate(['/sales-orders/new']);
  }

  viewSalesOrder(salesOrder: SalesOrderSummary): void {
    this.router.navigate(['/sales-orders', salesOrder.id]);
  }

  editSalesOrder(salesOrder: SalesOrderSummary): void {
    if (salesOrder.status === SalesOrderStatus.Draft) {
      this.router.navigate(['/sales-orders/edit', salesOrder.id]);
    } else {
      this.snackBar.open('Only draft orders can be edited', 'Close', { duration: 3000 });
    }
  }

  deleteSalesOrder(salesOrder: SalesOrderSummary): void {
    if (salesOrder.status !== SalesOrderStatus.Draft) {
      this.snackBar.open('Only draft orders can be deleted', 'Close', { duration: 3000 });
      return;
    }

    const confirmed = confirm(
      `Are you sure you want to delete order ${salesOrder.orderNumber}?`
    );

    if (confirmed) {
      this.salesOrderService.deleteSalesOrder(salesOrder.id).subscribe({
        next: () => {
          this.snackBar.open('Sales order deleted successfully', 'Close', { duration: 3000 });
          this.loadSalesOrders();
          this.loadStatistics();
        },
        error: (error) => {
          console.error('Error deleting sales order:', error);
          this.snackBar.open(
            error.error?.message || 'Error deleting sales order',
            'Close',
            { duration: 3000 }
          );
        }
      });
    }
  }

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

  canEdit(salesOrder: SalesOrderSummary): boolean {
    return salesOrder.status === SalesOrderStatus.Draft;
  }

  canDelete(salesOrder: SalesOrderSummary): boolean {
    return salesOrder.status === SalesOrderStatus.Draft;
  }
}
