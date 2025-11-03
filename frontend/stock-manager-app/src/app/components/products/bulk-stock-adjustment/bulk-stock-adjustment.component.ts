import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { ProductService } from '../../../services/product.service';
import { Product } from '../../../models/product.model';

interface ProductAdjustment {
  product: Product;
  newStock: number | null;
  changed: boolean;
}

@Component({
  selector: 'app-bulk-stock-adjustment',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatInputModule,
    MatFormFieldModule
  ],
  templateUrl: './bulk-stock-adjustment.component.html',
  styleUrl: './bulk-stock-adjustment.component.scss'
})
export class BulkStockAdjustmentComponent implements OnInit {
  adjustments: ProductAdjustment[] = [];
  loading = false;
  saving = false;
  errorMessage = '';
  displayedColumns: string[] = ['sku', 'name', 'currentStock', 'newStock'];

  constructor(
    private productService: ProductService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading = true;
    this.productService.getAll().subscribe({
      next: (products) => {
        // Show all products for bulk adjustment
        this.adjustments = products.map(product => ({
          product,
          newStock: null,
          changed: false
        }));
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.errorMessage = 'Failed to load products. Please try again.';
        this.loading = false;
      }
    });
  }

  onStockChange(adjustment: ProductAdjustment): void {
    if (adjustment.newStock !== null && adjustment.newStock !== adjustment.product.currentStock) {
      adjustment.changed = true;
    } else {
      adjustment.changed = false;
      adjustment.newStock = null;
    }
  }

  getChangedAdjustments(): ProductAdjustment[] {
    return this.adjustments.filter(a => a.changed && a.newStock !== null);
  }

  hasChanges(): boolean {
    return this.getChangedAdjustments().length > 0;
  }

  saveChanges(): void {
    const changedAdjustments = this.getChangedAdjustments();
    if (changedAdjustments.length === 0) {
      return;
    }

    this.saving = true;
    this.errorMessage = '';

    // Create bulk adjustment request
    const bulkAdjustment = {
      adjustments: changedAdjustments.map(adj => ({
        productId: adj.product.id,
        newStock: adj.newStock!
      })),
      reason: 'Bulk stock adjustment'
    };

    this.productService.bulkAdjustStock(bulkAdjustment).subscribe({
      next: (response) => {
        this.saving = false;
        // Navigate back to products page on success
        this.router.navigate(['/products']);
      },
      error: (error: any) => {
        console.error('Error updating stock:', error);
        this.saving = false;
        this.errorMessage = error.error?.message || 'Failed to update stock. Please try again.';
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/products']);
  }
}
