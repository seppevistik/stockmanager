import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { PurchaseOrderService } from '../../../services/purchase-order.service';
import { CompanyService } from '../../../services/company.service';
import { ProductService } from '../../../services/product.service';
import { Company } from '../../../models/company.model';
import { Product } from '../../../models/product.model';

@Component({
  selector: 'app-purchase-order-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './purchase-order-form.component.html',
  styleUrl: './purchase-order-form.component.scss'
})
export class PurchaseOrderFormComponent implements OnInit {
  purchaseOrderForm!: FormGroup;
  suppliers: Company[] = [];
  products: Product[] = [];
  loading = false;
  saving = false;
  isEditMode = false;
  purchaseOrderId?: number;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private purchaseOrderService: PurchaseOrderService,
    private companyService: CompanyService,
    private productService: ProductService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.loadSuppliers();
    this.loadProducts();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.purchaseOrderId = +id;
      this.loadPurchaseOrder(+id);
    } else {
      // Add initial line item for new PO
      this.addLineItem();
    }
  }

  initForm(): void {
    this.purchaseOrderForm = this.fb.group({
      companyId: [null, Validators.required],
      expectedDeliveryDate: [null],
      taxAmount: [0, [Validators.required, Validators.min(0)]],
      shippingCost: [0, [Validators.required, Validators.min(0)]],
      notes: [''],
      supplierReference: [''],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.purchaseOrderForm.get('lines') as FormArray;
  }

  createLineItem(): FormGroup {
    return this.fb.group({
      productId: [null, Validators.required],
      quantityOrdered: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      notes: ['']
    });
  }

  addLineItem(): void {
    this.lines.push(this.createLineItem());
  }

  removeLineItem(index: number): void {
    if (this.lines.length > 1) {
      this.lines.removeAt(index);
    }
  }

  onProductSelect(index: number): void {
    const productId = this.lines.at(index).get('productId')?.value;
    const product = this.products.find(p => p.id === productId);

    if (product) {
      // Set default price from product cost
      this.lines.at(index).patchValue({
        unitPrice: product.costPerUnit
      });
    }
  }

  loadSuppliers(): void {
    this.companyService.getSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers = suppliers;
      },
      error: (error) => {
        console.error('Error loading suppliers:', error);
        this.snackBar.open('Error loading suppliers', 'Close', { duration: 3000 });
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

  loadPurchaseOrder(id: number): void {
    this.loading = true;
    this.purchaseOrderService.getById(id).subscribe({
      next: (po) => {
        // Only allow editing draft orders
        if (po.status !== 'Draft') {
          this.snackBar.open('Only draft purchase orders can be edited', 'Close', { duration: 3000 });
          this.router.navigate(['/purchase-orders']);
          return;
        }

        this.purchaseOrderForm.patchValue({
          companyId: po.companyId,
          expectedDeliveryDate: po.expectedDeliveryDate,
          taxAmount: po.taxAmount,
          shippingCost: po.shippingCost,
          notes: po.notes,
          supplierReference: po.supplierReference
        });

        // Load line items
        this.lines.clear();
        po.lines.forEach(line => {
          this.lines.push(this.fb.group({
            productId: [line.productId, Validators.required],
            quantityOrdered: [line.quantityOrdered, [Validators.required, Validators.min(1)]],
            unitPrice: [line.unitPrice, [Validators.required, Validators.min(0)]],
            notes: [line.notes]
          }));
        });

        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading purchase order:', error);
        this.snackBar.open('Error loading purchase order', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  calculateSubTotal(): number {
    let subTotal = 0;
    this.lines.controls.forEach(line => {
      const qty = line.get('quantityOrdered')?.value || 0;
      const price = line.get('unitPrice')?.value || 0;
      subTotal += qty * price;
    });
    return subTotal;
  }

  calculateTotal(): number {
    const subTotal = this.calculateSubTotal();
    const tax = this.purchaseOrderForm.get('taxAmount')?.value || 0;
    const shipping = this.purchaseOrderForm.get('shippingCost')?.value || 0;
    return subTotal + tax + shipping;
  }

  getLineTotal(index: number): number {
    const line = this.lines.at(index);
    const qty = line.get('quantityOrdered')?.value || 0;
    const price = line.get('unitPrice')?.value || 0;
    return qty * price;
  }

  onSubmit(): void {
    if (this.purchaseOrderForm.invalid) {
      this.errorMessage = 'Please fill in all required fields';
      return;
    }

    if (this.lines.length === 0) {
      this.errorMessage = 'Please add at least one line item';
      return;
    }

    this.saving = true;
    this.errorMessage = '';

    const formValue = this.purchaseOrderForm.value;
    const request = {
      ...formValue,
      expectedDeliveryDate: formValue.expectedDeliveryDate || undefined
    };

    if (this.isEditMode && this.purchaseOrderId) {
      // Update existing PO
      this.purchaseOrderService.update(this.purchaseOrderId, request).subscribe({
        next: () => {
          this.snackBar.open('Purchase order updated successfully', 'Close', { duration: 3000 });
          this.router.navigate(['/purchase-orders']);
        },
        error: (error) => {
          console.error('Error updating purchase order:', error);
          this.errorMessage = error.error?.message || 'Error updating purchase order';
          this.saving = false;
        }
      });
    } else {
      // Create new PO
      this.purchaseOrderService.create(request).subscribe({
        next: (po) => {
          this.snackBar.open('Purchase order created successfully', 'Close', { duration: 3000 });
          this.router.navigate(['/purchase-orders', po.id]);
        },
        error: (error) => {
          console.error('Error creating purchase order:', error);
          this.errorMessage = error.error?.message || 'Error creating purchase order';
          this.saving = false;
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/purchase-orders']);
  }
}
