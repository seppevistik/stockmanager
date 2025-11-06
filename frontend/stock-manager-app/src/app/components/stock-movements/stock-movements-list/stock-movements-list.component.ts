import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { StockMovementService } from '../../../services/stock-movement.service';
import { StockMovement, StockMovementType } from '../../../models/stock-movement.model';

@Component({
  selector: 'app-stock-movements-list',
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
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule
  ],
  templateUrl: './stock-movements-list.component.html',
  styleUrl: './stock-movements-list.component.scss'
})
export class StockMovementsListComponent implements OnInit {
  movements: StockMovement[] = [];
  filteredMovements: StockMovement[] = [];
  loading = false;
  searchTerm = '';
  selectedType: number = -1;
  startDate: Date | null = null;
  endDate: Date | null = null;
  displayedColumns: string[] = ['date', 'product', 'type', 'quantity', 'previousStock', 'newStock', 'user', 'reason'];

  movementTypes = [
    { value: -1, label: 'All Types' },
    { value: StockMovementType.StockIn, label: 'Stock In' },
    { value: StockMovementType.StockOut, label: 'Stock Out' },
    { value: StockMovementType.StockAdjustment, label: 'Adjustment' },
    { value: StockMovementType.StockTransfer, label: 'Transfer' }
  ];

  constructor(private stockMovementService: StockMovementService) {}

  ngOnInit(): void {
    this.loadMovements();
  }

  loadMovements(): void {
    this.loading = true;
    this.stockMovementService.getAll(this.startDate || undefined, this.endDate || undefined).subscribe({
      next: (movements) => {
        this.movements = movements;
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading stock movements:', error);
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.movements];

    // Filter by search term
    if (this.searchTerm) {
      const search = this.searchTerm.toLowerCase().trim();
      filtered = filtered.filter(movement =>
        movement.productName.toLowerCase().includes(search) ||
        movement.productSKU.toLowerCase().includes(search) ||
        movement.userName.toLowerCase().includes(search) ||
        (movement.reason && movement.reason.toLowerCase().includes(search)) ||
        (movement.notes && movement.notes.toLowerCase().includes(search))
      );
    }

    // Filter by movement type
    if (this.selectedType !== -1) { //all
      filtered = filtered.filter(movement => movement.movementType === this.selectedType);
    }

    this.filteredMovements = filtered;
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  onDateRangeChange(): void {
    this.loadMovements();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedType = -1;
    this.startDate = null;
    this.endDate = null;
    this.loadMovements();
  }

  getMovementTypeClass(type: StockMovementType): string {
    switch (type) {
      case StockMovementType.StockIn:
        return 'type-stockin';
      case StockMovementType.StockOut:
        return 'type-stockout';
      case StockMovementType.StockAdjustment:
        return 'type-adjustment';
      case StockMovementType.StockTransfer:
        return 'type-transfer';
      default:
        return '';
    }
  }

  getMovementTypeLabel(type: StockMovementType): string {
    switch (type) {
      case StockMovementType.StockIn:
        return 'Stock In';
      case StockMovementType.StockOut:
        return 'Stock Out';
      case StockMovementType.StockAdjustment:
        return 'Adjustment';
      case StockMovementType.StockTransfer:
        return 'Transfer';
      default:
        return type;
    }
  }

  getQuantityClass(movement: StockMovement): string {
    if (movement.movementType === StockMovementType.StockIn) {
      return 'quantity-increase';
    } else if (movement.movementType === StockMovementType.StockOut) {
      return 'quantity-decrease';
    }
    return '';
  }

  formatQuantity(movement: StockMovement): string {
    const prefix = movement.movementType === StockMovementType.StockIn ? '+' :
                   movement.movementType === StockMovementType.StockOut ? '-' : '';
    return `${prefix}${movement.quantity}`;
  }
}
