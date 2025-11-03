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
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../services/product.service';
import { Product, ProductStatus } from '../../../models/product.model';

@Component({
  selector: 'app-products-list',
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
    MatFormFieldModule
  ],
  templateUrl: './products-list.component.html',
  styleUrl: './products-list.component.scss'
})
export class ProductsListComponent implements OnInit {
  products: Product[] = [];
  filteredProducts: Product[] = [];
  loading = false;
  searchTerm = '';
  displayedColumns: string[] = ['sku', 'name', 'category', 'currentStock', 'minimumStock', 'costPerUnit', 'totalValue', 'status', 'actions'];

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
        this.products = products;
        this.filteredProducts = products;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.loading = false;
      }
    });
  }

  filterProducts(): void {
    const search = this.searchTerm.toLowerCase().trim();
    if (!search) {
      this.filteredProducts = this.products;
      return;
    }

    this.filteredProducts = this.products.filter(product =>
      product.name.toLowerCase().includes(search) ||
      product.sku.toLowerCase().includes(search) ||
      (product.categoryName && product.categoryName.toLowerCase().includes(search)) ||
      (product.supplier && product.supplier.toLowerCase().includes(search))
    );
  }

  getStockStatusClass(product: Product): string {
    if (product.currentStock === 0) {
      return 'out-of-stock';
    } else if (product.currentStock <= product.minimumStockLevel) {
      return 'low-stock';
    }
    return 'in-stock';
  }

  getStockStatusText(product: Product): string {
    if (product.currentStock === 0) {
      return 'Out of Stock';
    } else if (product.currentStock <= product.minimumStockLevel) {
      return 'Low Stock';
    }
    return 'In Stock';
  }

  addProduct(): void {
    this.router.navigate(['/products/new']);
  }

  editProduct(product: Product): void {
    this.router.navigate(['/products/edit', product.id]);
  }

  deleteProduct(product: Product): void {
    if (confirm(`Are you sure you want to delete "${product.name}"?`)) {
      this.productService.delete(product.id).subscribe({
        next: () => {
          this.loadProducts();
        },
        error: (error) => {
          console.error('Error deleting product:', error);
          alert('Failed to delete product. Please try again.');
        }
      });
    }
  }
}
