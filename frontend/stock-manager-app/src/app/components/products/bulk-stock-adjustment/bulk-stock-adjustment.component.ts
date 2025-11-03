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
        // Filter for active products only
        const activeProducts = products.filter(p => p.status === 'Active');
        this.adjustments = activeProducts.map(product => ({
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

    // Create an array of update requests
    const updateRequests = changedAdjustments.map(adj => {
      const productData = {
        name: adj.product.name,
        description: adj.product.description || '',
        sku: adj.product.sku,
        unitOfMeasurement: adj.product.unitOfMeasurement,
        supplier: adj.product.supplier || '',
        minimumStockLevel: adj.product.minimumStockLevel,
        initialStock: adj.newStock!, // This will update the current stock
        costPerUnit: adj.product.costPerUnit,
        location: adj.product.location || ''
      };
      return this.productService.update(adj.product.id, productData);
    });

    // Execute all updates
    let completed = 0;
    let failed = 0;

    updateRequests.forEach((request, index) => {
      request.subscribe({
        next: () => {
          completed++;
          if (completed + failed === updateRequests.length) {
            this.handleBulkUpdateComplete(completed, failed);
          }
        },
        error: (error: any) => {
          console.error('Error updating product:', error);
          failed++;
          if (completed + failed === updateRequests.length) {
            this.handleBulkUpdateComplete(completed, failed);
          }
        }
      });
    });
  }

  handleBulkUpdateComplete(completed: number, failed: number): void {
    this.saving = false;
    if (failed === 0) {
      // All updates successful, navigate back to products
      this.router.navigate(['/products']);
    } else {
      this.errorMessage = `Updated ${completed} products successfully. ${failed} products failed to update.`;
      if (completed > 0) {
        // Reload to show updated values
        this.loadProducts();
      }
    }
  }

  cancel(): void {
    this.router.navigate(['/products']);
  }
}
