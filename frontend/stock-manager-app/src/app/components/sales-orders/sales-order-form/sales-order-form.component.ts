import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
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
import { MatStepperModule } from '@angular/material/stepper';
import { MatDividerModule } from '@angular/material/divider';
import { Observable, startWith, map } from 'rxjs';
import { SalesOrderService } from '../../../services/sales-order.service';
import { CustomerService } from '../../../services/customer.service';
import { ProductService } from '../../../services/product.service';
import {
  CreateSalesOrderRequest,
  UpdateSalesOrderRequest,
  Priority,
  SalesOrder
} from '../../../models/sales-order.model';
import { Customer } from '../../../models/customer.model';
import { Product } from '../../../models/product.model';

// Custom validator for future dates
function futureDateValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;

  const selectedDate = new Date(control.value);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  selectedDate.setHours(0, 0, 0, 0);

  return selectedDate < today ? { pastDate: true } : null;
}

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
    MatAutocompleteModule,
    MatStepperModule,
    MatDividerModule
  ],
  templateUrl: './sales-order-form.component.html',
  styleUrl: './sales-order-form.component.scss'
})
export class SalesOrderFormComponent implements OnInit {
  // Form groups for each step
  customerDetailsForm!: FormGroup;
  productsForm!: FormGroup;

  isEditMode = false;
  salesOrderId?: number;
  loading = false;
  submitting = false;

  customers: Customer[] = [];
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
    private customerService: CustomerService,
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
    this.productsForm.valueChanges.subscribe(() => {
      this.calculateTotals();
    });
  }

  initializeForm(): void {
    // Step 1: Customer Details
    this.customerDetailsForm = this.fb.group({
      customerId: [null],
      customerReference: [''],
      priority: [Priority.Normal, Validators.required],

      // Shipping Information
      shipToName: ['', Validators.required],
      shipToAddress: ['', Validators.required],
      shipToCity: ['', Validators.required],
      shipToState: ['', Validators.required],
      shipToPostalCode: ['', Validators.required],
      shipToCountry: ['USA', Validators.required],
      shipToPhone: [''],
      shippingMethod: [''],

      // Dates
      requiredDate: [null, futureDateValidator],
      promisedDate: [null, futureDateValidator],

      // Notes
      notes: [''],
      internalNotes: ['']
    });

    // Step 2: Products
    this.productsForm = this.fb.group({
      lines: this.fb.array([]),

      // Financial
      taxRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      shippingCost: [0, [Validators.required, Validators.min(0)]],
      discountAmount: [0, [Validators.required, Validators.min(0)]]
    });
  }

  get lines(): FormArray {
    return this.productsForm.get('lines') as FormArray;
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
      unitPrice: product.costPerUnit || 0
    });

    // Show stock availability warning
    const quantityOrdered = line.get('quantityOrdered')?.value || 1;
    if (product.currentStock < quantityOrdered) {
      this.snackBar.open(
        `Warning: ${product.name} has only ${product.currentStock} units in stock. Ordered: ${quantityOrdered}`,
        'Close',
        { duration: 5000, panelClass: ['warning-snackbar'] }
      );
    } else if (product.currentStock - quantityOrdered < product.minimumStockLevel) {
      this.snackBar.open(
        `Warning: ${product.name} will be below minimum stock level (${product.minimumStockLevel}) after this order.`,
        'Close',
        { duration: 5000, panelClass: ['warning-snackbar'] }
      );
    }
  }

  getProductStock(productId: number): number {
    const product = this.products.find(p => p.id === productId);
    return product?.currentStock || 0;
  }

  isStockAvailable(index: number): boolean {
    const line = this.lines.at(index);
    const productId = line.get('productId')?.value;
    const quantityOrdered = line.get('quantityOrdered')?.value || 0;
    const product = this.products.find(p => p.id === productId);
    return product ? product.currentStock >= quantityOrdered : true;
  }

  getStockWarning(index: number): string {
    const line = this.lines.at(index);
    const productId = line.get('productId')?.value;
    const quantityOrdered = line.get('quantityOrdered')?.value || 0;
    const product = this.products.find(p => p.id === productId);

    if (!product || !productId) return '';

    if (product.currentStock < quantityOrdered) {
      return `Insufficient stock! Available: ${product.currentStock}`;
    } else if (product.currentStock - quantityOrdered < product.minimumStockLevel) {
      return `Below min. stock level after order (Min: ${product.minimumStockLevel})`;
    } else {
      return `In stock: ${product.currentStock} available`;
    }
  }

  displayProduct(productId: number): string {
    if (!productId) return '';
    const product = this.products.find(p => p.id === productId);
    return product ? `${product.sku} - ${product.name}` : '';
  }

  loadCustomers(): void {
    this.customerService.getCustomers({ isActive: true, pageSize: 1000 }).subscribe({
      next: (result) => {
        this.customers = result.items;
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
    // Populate customer details form
    this.customerDetailsForm.patchValue({
      customerId: order.customerId,
      customerReference: order.customerReference,
      priority: order.priority,
      shipToName: order.shipToName,
      shipToAddress: order.shipToAddress,
      shipToCity: order.shipToCity,
      shipToState: order.shipToState,
      shipToPostalCode: order.shipToPostalCode,
      shipToCountry: order.shipToCountry,
      shipToPhone: order.shipToPhone,
      shippingMethod: order.shippingMethod,
      requiredDate: order.requiredDate ? new Date(order.requiredDate) : null,
      promisedDate: order.promisedDate ? new Date(order.promisedDate) : null,
      notes: order.notes,
      internalNotes: order.internalNotes
    });

    // Populate products form
    this.productsForm.patchValue({
      taxRate: order.taxRate,
      shippingCost: order.shippingCost,
      discountAmount: order.discountAmount
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
      this.customerDetailsForm.patchValue({
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
    const taxRate = this.productsForm.get('taxRate')?.value || 0;
    return subtotal * (taxRate / 100);
  }

  getTotal(): number {
    const subtotal = this.getSubTotal();
    const tax = this.getTaxAmount();
    const shipping = this.productsForm.get('shippingCost')?.value || 0;
    const discount = this.productsForm.get('discountAmount')?.value || 0;
    return subtotal + tax + shipping - discount;
  }

  onSubmit(): void {
    if (this.customerDetailsForm.invalid || this.productsForm.invalid) {
      this.customerDetailsForm.markAllAsTouched();
      this.productsForm.markAllAsTouched();

      // Log validation errors for debugging
      console.log('Customer Details Form Errors:', this.customerDetailsForm.errors);
      console.log('Products Form Errors:', this.productsForm.errors);

      this.snackBar.open('Please fill in all required fields', 'Close', { duration: 3000 });
      return;
    }

    if (this.lines.length === 0) {
      this.snackBar.open('At least one line item is required', 'Close', { duration: 3000 });
      return;
    }

    this.submitting = true;

    // Combine both forms
    const formValue = {
      ...this.customerDetailsForm.value,
      ...this.productsForm.value
    };

    console.log('Submitting order with data:', formValue);

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
          console.error('Error details:', error.error);
          this.snackBar.open(
            error.error?.message || error.error?.title || 'Error updating sales order',
            'Close',
            { duration: 5000 }
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
          console.error('Error details:', error.error);

          // Show detailed error message
          let errorMessage = 'Error creating sales order';
          if (error.error?.errors) {
            const errorMessages = Object.values(error.error.errors).flat();
            errorMessage = errorMessages.join(', ');
          } else if (error.error?.message) {
            errorMessage = error.error.message;
          } else if (error.error?.title) {
            errorMessage = error.error.title;
          }

          this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
          this.submitting = false;
        }
      });
    }
  }

  // Helpers for overview step
  getSelectedCustomer(): Customer | undefined {
    const customerId = this.customerDetailsForm.get('customerId')?.value;
    return this.customers.find(c => c.id === customerId);
  }

  getSelectedProduct(productId: number): Product | undefined {
    return this.products.find(p => p.id === productId);
  }

  getPriorityLabel(): string {
    const priorityValue = this.customerDetailsForm.get('priority')?.value;
    return this.priorities.find(p => p.value === priorityValue)?.label || 'Normal';
  }

  getShipToName(): string {
    return this.customerDetailsForm.get('shipToName')?.value || '';
  }

  getShipToAddress(): string {
    return this.customerDetailsForm.get('shipToAddress')?.value || '';
  }

  getShipToCity(): string {
    return this.customerDetailsForm.get('shipToCity')?.value || '';
  }

  getShipToState(): string {
    return this.customerDetailsForm.get('shipToState')?.value || '';
  }

  getShipToPostalCode(): string {
    return this.customerDetailsForm.get('shipToPostalCode')?.value || '';
  }

  getShipToCountry(): string {
    return this.customerDetailsForm.get('shipToCountry')?.value || '';
  }

  getShipToPhone(): string {
    return this.customerDetailsForm.get('shipToPhone')?.value || '';
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
