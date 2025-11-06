import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { PurchaseOrderService } from '../../../services/purchase-order.service';
import { CompanyService } from '../../../services/company.service';
import { PurchaseOrder, PurchaseOrderStatus } from '../../../models/purchase-order.model';
import { Company } from '../../../models/company.model';

@Component({
  selector: 'app-purchase-orders-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatDialogModule,
    MatSnackBarModule
  ],
  templateUrl: './purchase-orders-list.component.html',
  styleUrl: './purchase-orders-list.component.scss'
})
export class PurchaseOrdersListComponent implements OnInit {
  purchaseOrders: PurchaseOrder[] = [];
  filteredPurchaseOrders: PurchaseOrder[] = [];
  suppliers: Company[] = [];
  loading = false;
  searchTerm = '';
  statusFilter: PurchaseOrderStatus | '' = '';
  supplierFilter: number | '' = '';

  displayedColumns: string[] = ['orderNumber', 'orderDate', 'companyName', 'expectedDeliveryDate', 'status', 'totalAmount', 'actions'];

  statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: PurchaseOrderStatus.Draft, label: 'Draft' },
    { value: PurchaseOrderStatus.Submitted, label: 'Submitted' },
    { value: PurchaseOrderStatus.Confirmed, label: 'Confirmed' },
    { value: PurchaseOrderStatus.Receiving, label: 'Receiving' },
    { value: PurchaseOrderStatus.PartiallyReceived, label: 'Partially Received' },
    { value: PurchaseOrderStatus.Completed, label: 'Completed' },
    { value: PurchaseOrderStatus.Cancelled, label: 'Cancelled' }
  ];

  constructor(
    private purchaseOrderService: PurchaseOrderService,
    private companyService: CompanyService,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadSuppliers();
    this.loadPurchaseOrders();
  }

  loadSuppliers(): void {
    this.companyService.getSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers = suppliers;
      },
      error: (error) => {
        console.error('Error loading suppliers:', error);
      }
    });
  }

  loadPurchaseOrders(): void {
    this.loading = true;
    const filter = {
      status: this.statusFilter || undefined,
      companyId: this.supplierFilter || undefined,
      search: this.searchTerm || undefined
    };

    this.purchaseOrderService.getAll(filter).subscribe({
      next: (orders) => {
        this.purchaseOrders = orders;
        this.filteredPurchaseOrders = orders;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading purchase orders:', error);
        this.snackBar.open('Error loading purchase orders', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.loadPurchaseOrders();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.statusFilter = '';
    this.supplierFilter = '';
    this.loadPurchaseOrders();
  }

  createPurchaseOrder(): void {
    this.router.navigate(['/purchase-orders/new']);
  }

  viewPurchaseOrder(id: number): void {
    this.router.navigate(['/purchase-orders', id]);
  }

  editPurchaseOrder(id: number): void {
    this.router.navigate(['/purchase-orders/edit', id]);
  }

  deletePurchaseOrder(id: number): void {
    if (confirm('Are you sure you want to delete this purchase order?')) {
      this.purchaseOrderService.delete(id).subscribe({
        next: () => {
          this.snackBar.open('Purchase order deleted successfully', 'Close', { duration: 3000 });
          this.loadPurchaseOrders();
        },
        error: (error) => {
          console.error('Error deleting purchase order:', error);
          this.snackBar.open('Error deleting purchase order', 'Close', { duration: 3000 });
        }
      });
    }
  }

  getStatusColor(status: PurchaseOrderStatus): string {
    switch (status) {
      case PurchaseOrderStatus.Draft:
        return 'default';
      case PurchaseOrderStatus.Submitted:
        return 'primary';
      case PurchaseOrderStatus.Confirmed:
        return 'accent';
      case PurchaseOrderStatus.Receiving:
      case PurchaseOrderStatus.PartiallyReceived:
        return 'warn';
      case PurchaseOrderStatus.Completed:
        return 'success';
      case PurchaseOrderStatus.Cancelled:
        return 'error';
      default:
        return 'default';
    }
  }

  getStatusLabel(status: PurchaseOrderStatus): string {
    const statusOption = this.statusOptions.find(opt => opt.value === status);
    return statusOption ? statusOption.label : status.toString();
  }

  canEdit(order: PurchaseOrder): boolean {
    return order.status === PurchaseOrderStatus.Draft;
  }

  canDelete(order: PurchaseOrder): boolean {
    return order.status === PurchaseOrderStatus.Draft;
  }
}
