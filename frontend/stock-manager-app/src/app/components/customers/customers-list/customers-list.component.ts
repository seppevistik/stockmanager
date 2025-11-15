import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatChipsModule } from '@angular/material/chips';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { CustomerService } from '../../../services/customer.service';
import { Customer, CustomerListQuery } from '../../../models/customer.model';

@Component({
  selector: 'app-customers-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatChipsModule,
    MatPaginatorModule,
    MatSelectModule,
    FormsModule
  ],
  templateUrl: './customers-list.component.html',
  styleUrl: './customers-list.component.scss'
})
export class CustomersListComponent implements OnInit {
  customers: Customer[] = [];
  displayedColumns: string[] = ['name', 'type', 'email', 'phone', 'city', 'status', 'actions'];
  loading = false;

  // Pagination
  totalCount = 0;
  pageSize = 20;
  pageIndex = 0;

  // Filters
  searchTerm = '';
  filterType: boolean | null = null;
  filterActive: boolean | null = null;

  constructor(
    private customerService: CustomerService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCustomers();
  }

  loadCustomers(): void {
    this.loading = true;

    const query: CustomerListQuery = {
      page: this.pageIndex + 1,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm || undefined,
      isCompany: this.filterType !== null ? this.filterType : undefined,
      isActive: this.filterActive !== null ? this.filterActive : undefined
    };

    this.customerService.getCustomers(query).subscribe({
      next: (result) => {
        this.customers = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading customers:', error);
        this.loading = false;
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadCustomers();
  }

  applyFilters(): void {
    this.pageIndex = 0;
    this.loadCustomers();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.filterType = null;
    this.filterActive = null;
    this.pageIndex = 0;
    this.loadCustomers();
  }

  addCustomer(): void {
    this.router.navigate(['/customers/new']);
  }

  editCustomer(id: number): void {
    this.router.navigate(['/customers/edit', id]);
  }

  deleteCustomer(id: number, name: string): void {
    if (confirm(`Are you sure you want to delete customer "${name}"?`)) {
      this.customerService.deleteCustomer(id).subscribe({
        next: () => {
          this.loadCustomers();
        },
        error: (error) => {
          console.error('Error deleting customer:', error);
          alert('Failed to delete customer');
        }
      });
    }
  }
}
