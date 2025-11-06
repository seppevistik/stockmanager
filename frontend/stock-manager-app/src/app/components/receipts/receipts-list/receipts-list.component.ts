import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FormsModule } from '@angular/forms';
import { ReceiptService } from '../../../services/receipt.service';
import { Receipt, ReceiptStatus } from '../../../models/receipt.model';

@Component({
  selector: 'app-receipts-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './receipts-list.component.html',
  styleUrl: './receipts-list.component.scss'
})
export class ReceiptsListComponent implements OnInit {
  receipts: Receipt[] = [];
  filteredReceipts: Receipt[] = [];
  loading = false;
  errorMessage = '';

  displayedColumns: string[] = ['receiptNumber', 'purchaseOrderNumber', 'companyName', 'receiptDate', 'receivedByName', 'status', 'hasVariances', 'actions'];

  // Filter properties
  statusFilter: string = 'All';
  searchTerm: string = '';

  statuses = [
    { value: 'All', label: 'All Statuses' },
    { value: ReceiptStatus.InProgress, label: 'In Progress' },
    { value: ReceiptStatus.PendingValidation, label: 'Pending Validation' },
    { value: ReceiptStatus.Validated, label: 'Validated' },
    { value: ReceiptStatus.Completed, label: 'Completed' },
    { value: ReceiptStatus.Rejected, label: 'Rejected' }
  ];

  constructor(
    private receiptService: ReceiptService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadReceipts();
  }

  loadReceipts(): void {
    this.loading = true;
    this.errorMessage = '';

    this.receiptService.getAll().subscribe({
      next: (receipts) => {
        this.receipts = receipts;
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading receipts:', error);
        this.errorMessage = 'Error loading receipts. Please try again.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.receipts];

    // Apply status filter
    if (this.statusFilter !== 'All') {
      filtered = filtered.filter(r => r.status === this.statusFilter);
    }

    // Apply search filter
    if (this.searchTerm) {
      const search = this.searchTerm.toLowerCase();
      filtered = filtered.filter(r =>
        r.receiptNumber.toLowerCase().includes(search) ||
        r.purchaseOrderNumber.toLowerCase().includes(search) ||
        r.companyName.toLowerCase().includes(search) ||
        r.receivedByName.toLowerCase().includes(search)
      );
    }

    this.filteredReceipts = filtered;
  }

  onStatusFilterChange(): void {
    this.applyFilters();
  }

  onSearch(): void {
    this.applyFilters();
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
    const statusOption = this.statuses.find(s => s.value === status);
    return statusOption ? statusOption.label : status.toString();
  }

  viewReceipt(receipt: Receipt): void {
    this.router.navigate(['/receipts', receipt.id]);
  }

  viewPurchaseOrder(receipt: Receipt): void {
    this.router.navigate(['/purchase-orders', receipt.purchaseOrderId]);
  }

  createReceipt(): void {
    this.router.navigate(['/receipts', 'new']);
  }

  getPendingValidationCount(): number {
    return this.receipts.filter(r => r.status === ReceiptStatus.PendingValidation).length;
  }

  showPendingValidationOnly(): void {
    this.statusFilter = ReceiptStatus.PendingValidation;
    this.applyFilters();
  }

  deleteReceipt(receipt: Receipt): void {
    if (receipt.status === ReceiptStatus.Completed) {
      this.snackBar.open('Cannot delete completed receipts', 'Close', { duration: 3000 });
      return;
    }

    if (confirm(`Are you sure you want to delete receipt ${receipt.receiptNumber}?`)) {
      this.receiptService.delete(receipt.id).subscribe({
        next: () => {
          this.snackBar.open('Receipt deleted successfully', 'Close', { duration: 3000 });
          this.loadReceipts();
        },
        error: (error) => {
          console.error('Error deleting receipt:', error);
          this.snackBar.open('Error deleting receipt', 'Close', { duration: 3000 });
        }
      });
    }
  }

  canDelete(receipt: Receipt): boolean {
    return receipt.status !== ReceiptStatus.Completed;
  }
}
