import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { Observable, startWith, map } from 'rxjs';
import { SalesOrderService } from '../../../services/sales-order.service';
import { CompanyService } from '../../../services/company.service';
import { ProductService } from '../../../services/product.service';
import {
  CreateSalesOrderRequest,
  UpdateSalesOrderRequest,
  Priority,
  SalesOrder
} from '../../../models/sales-order.model';
import { Company } from '../../../models/company.model';
import { Product } from '../../../models/product.model';

@Component({
  selector: 'app-sales-order-form',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTableModule,
    MatTooltipModule,
    MatAutocompleteModule
  ],
  templateUrl: './sales-order-form.component.html',
  styleUrl: './sales-order-form.component.scss'
})
export class SalesOrderFormComponent implements OnInit {
  form!: FormGroup;
  isEditMode = false;
  salesOrderId?: number;
  loading = false;
  submitting = false;

  customers: Company[] = [];
  products: Product[] = [];
  filteredProducts: Observable<Product[]>[] = [];

  priorities = [
    { value: Priority.Low, label: 'Low' },
    { value: Priority.Normal, label: 'Normal' },
    { value: Priority.High, label: 'High' },
    { value: Priority.Urgent, label: 'Urgent' }
  ];

  lineDisplayedColumns = ['product', 'quantity', 'unitPrice', 'discount', 'total', 'actions'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private salesOrderService: SalesOrderService,
    private companyService: CompanyService,
    private productService: ProductService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.loadCustomers();
    this.loadProducts();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.salesOrderId = +id;
      this.loadSalesOrder(+id);
    } else {
      // Add one empty line by default for new orders
      this.addLine();
    }

    // Watch for changes to recalculate totals
    this.form.valueChanges.subscribe(() => {
      this.calculateTotals();
    });
  }

  initializeForm(): void {
    this.form = this.fb.group({
      customerId: ['', Validators.required],

      // Shipping Information
      shipToName: ['', Validators.required],
      shipToAddress: ['', Validators.required],
      shipToCity: ['', Validators.required],
      shipToState: ['', Validators.required],
      shipToPostalCode: ['', Validators.required],
      shipToCountry: ['USA', Validators.required],
      shipToPhone: [''],

      // Financial
      taxRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      shippingCost: [0, [Validators.required, Validators.min(0)]],
      discountAmount: [0, [Validators.required, Validators.min(0)]],

      // Priority & Dates
      priority: [Priority.Normal, Validators.required],
      requiredDate: [null],
      promisedDate: [null],

      // Additional Info
      customerReference: [''],
      shippingMethod: [''],
      notes: [''],
      internalNotes: [''],

      // Order Lines
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  createLineFormGroup(): FormGroup {
    return this.fb.group({
      productId: ['', Validators.required],
      quantityOrdered: [1, [Validators.required, Validators.min(0.01)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      discountPercent: [0, [Validators.min(0), Validators.max(100)]],
      notes: ['']
    });
  }

  addLine(): void {
    const lineGroup = this.createLineFormGroup();
    this.lines.push(lineGroup);

    // Setup filtered products for this line
    const index = this.lines.length - 1;
    this.setupProductFilter(index);
  }

  removeLine(index: number): void {
    if (this.lines.length > 1) {
      this.lines.removeAt(index);
      this.filteredProducts.splice(index, 1);
    } else {
      this.snackBar.open('At least one line item is required', 'Close', { duration: 3000 });
    }
  }

  setupProductFilter(index: number): void {
    const productControl = this.lines.at(index).get('productId');
    if (productControl) {
      this.filteredProducts[index] = productControl.valueChanges.pipe(
        startWith(''),
        map(value => this._filterProducts(value))
      );
    }
  }

  private _filterProducts(value: any): Product[] {
    if (typeof value === 'number') {
      return this.products;
    }
    const filterValue = (value || '').toLowerCase();
    return this.products.filter(product =>
      product.name.toLowerCase().includes(filterValue) ||
      product.sku.toLowerCase().includes(filterValue)
    );
  }

  onProductSelected(index: number, product: Product): void {
    const line = this.lines.at(index);
    line.patchValue({
      productId: product.id,
      unitPrice: product.currentPrice || 0
    });
  }

  displayProduct(productId: number): string {
    if (!productId) return '';
    const product = this.products.find(p => p.id === productId);
    return product ? `${product.sku} - ${product.name}` : '';
  }

  loadCustomers(): void {
    this.companyService.getAll().subscribe({
      next: (companies) => {
        this.customers = companies.filter(c => c.type === 'Customer');
      },
      error: (error) => {
        console.error('Error loading customers:', error);
        this.snackBar.open('Error loading customers', 'Close', { duration: 3000 });
      }
    });
  }

  loadProducts(): void {
    this.productService.getAll().subscribe({
      next: (products) => {
        this.products = products;
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.snackBar.open('Error loading products', 'Close', { duration: 3000 });
      }
    });
  }

  loadSalesOrder(id: number): void {
    this.loading = true;
    this.salesOrderService.getSalesOrder(id).subscribe({
      next: (order) => {
        this.populateForm(order);
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

  populateForm(order: SalesOrder): void {
    this.form.patchValue({
      customerId: order.customerId,
      shipToName: order.shipToName,
      shipToAddress: order.shipToAddress,
      shipToCity: order.shipToCity,
      shipToState: order.shipToState,
      shipToPostalCode: order.shipToPostalCode,
      shipToCountry: order.shipToCountry,
      shipToPhone: order.shipToPhone,
      taxRate: order.taxRate,
      shippingCost: order.shippingCost,
      discountAmount: order.discountAmount,
      priority: order.priority,
      requiredDate: order.requiredDate ? new Date(order.requiredDate) : null,
      promisedDate: order.promisedDate ? new Date(order.promisedDate) : null,
      customerReference: order.customerReference,
      shippingMethod: order.shippingMethod,
      notes: order.notes,
      internalNotes: order.internalNotes
    });

    // Clear existing lines and add lines from order
    this.lines.clear();
    order.lines.forEach(line => {
      const lineGroup = this.fb.group({
        productId: [line.productId, Validators.required],
        quantityOrdered: [line.quantityOrdered, [Validators.required, Validators.min(0.01)]],
        unitPrice: [line.unitPrice, [Validators.required, Validators.min(0)]],
        discountPercent: [line.discountPercent, [Validators.min(0), Validators.max(100)]],
        notes: [line.notes || '']
      });
      this.lines.push(lineGroup);
      this.setupProductFilter(this.lines.length - 1);
    });
  }

  onCustomerChange(customerId: number): void {
    const customer = this.customers.find(c => c.id === customerId);
    if (customer) {
      // Auto-fill shipping address from customer if available
      this.form.patchValue({
        shipToName: customer.name,
        shipToAddress: customer.address || '',
        shipToCity: customer.city || '',
        shipToState: customer.state || '',
        shipToPostalCode: customer.postalCode || '',
        shipToCountry: customer.country || 'USA',
        shipToPhone: customer.phone || ''
      });
    }
  }

  calculateLineTotal(index: number): number {
    const line = this.lines.at(index).value;
    const subtotal = line.quantityOrdered * line.unitPrice;
    const discount = subtotal * (line.discountPercent / 100);
    return subtotal - discount;
  }

  calculateTotals(): void {
    // Totals are calculated automatically in the template
  }

  getSubTotal(): number {
    return this.lines.controls.reduce((sum, _, index) => {
      return sum + this.calculateLineTotal(index);
    }, 0);
  }

  getTaxAmount(): number {
    const subtotal = this.getSubTotal();
    const taxRate = this.form.get('taxRate')?.value || 0;
    return subtotal * (taxRate / 100);
  }

  getTotal(): number {
    const subtotal = this.getSubTotal();
    const tax = this.getTaxAmount();
    const shipping = this.form.get('shippingCost')?.value || 0;
    const discount = this.form.get('discountAmount')?.value || 0;
    return subtotal + tax + shipping - discount;
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snackBar.open('Please fill in all required fields', 'Close', { duration: 3000 });
      return;
    }

    if (this.lines.length === 0) {
      this.snackBar.open('At least one line item is required', 'Close', { duration: 3000 });
      return;
    }

    this.submitting = true;

    const formValue = this.form.value;

    if (this.isEditMode && this.salesOrderId) {
      const request: UpdateSalesOrderRequest = {
        ...formValue,
        lines: formValue.lines.map((line: any) => ({
          id: null, // For edit mode, we're replacing all lines
          ...line
        }))
      };

      this.salesOrderService.updateSalesOrder(this.salesOrderId, request).subscribe({
        next: () => {
          this.snackBar.open('Sales order updated successfully', 'Close', { duration: 3000 });
          this.router.navigate(['/sales-orders', this.salesOrderId]);
        },
        error: (error) => {
          console.error('Error updating sales order:', error);
          this.snackBar.open(
            error.error?.message || 'Error updating sales order',
            'Close',
            { duration: 3000 }
          );
          this.submitting = false;
        }
      });
    } else {
      const request: CreateSalesOrderRequest = formValue;

      this.salesOrderService.createSalesOrder(request).subscribe({
        next: (order) => {
          this.snackBar.open('Sales order created successfully', 'Close', { duration: 3000 });
          this.router.navigate(['/sales-orders', order.id]);
        },
        error: (error) => {
          console.error('Error creating sales order:', error);
          this.snackBar.open(
            error.error?.message || 'Error creating sales order',
            'Close',
            { duration: 3000 }
          );
          this.submitting = false;
        }
      });
    }
  }

  onCancel(): void {
    if (this.isEditMode && this.salesOrderId) {
      this.router.navigate(['/sales-orders', this.salesOrderId]);
    } else {
      this.router.navigate(['/sales-orders']);
    }
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }
}
