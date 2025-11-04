import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTableModule } from '@angular/material/table';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ProductService } from '../../../services/product.service';
import { CompanyService } from '../../../services/company.service';
import { CreateProductRequest, ProductSupplier } from '../../../models/product.model';
import { Company } from '../../../models/company.model';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatExpansionModule,
    MatTableModule,
    MatSelectModule,
    MatCheckboxModule,
    MatTooltipModule
  ],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss'
})
export class ProductFormComponent implements OnInit {
  productForm: FormGroup;
  supplierForm: FormGroup;
  loading = false;
  saving = false;
  isEditMode = false;
  productId?: number;
  errorMessage = '';

  availableSuppliers: Company[] = [];
  productSuppliers: ProductSupplier[] = [];
  editingSupplier: ProductSupplier | null = null;
  showSupplierForm = false;
  supplierColumns = ['companyName', 'price', 'leadTime', 'primary', 'actions'];

  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private companyService: CompanyService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', Validators.maxLength(1000)],
      sku: ['', [Validators.required, Validators.maxLength(50)]],
      unitOfMeasurement: ['', [Validators.required, Validators.maxLength(20)]],
      supplier: ['', Validators.maxLength(200)],
      minimumStockLevel: [0, [Validators.required, Validators.min(0)]],
      initialStock: [0, [Validators.required, Validators.min(0)]],
      costPerUnit: [0, [Validators.required, Validators.min(0)]],
      location: ['', Validators.maxLength(100)]
    });

    this.supplierForm = this.fb.group({
      companyId: ['', Validators.required],
      supplierPrice: [null],
      supplierProductCode: [''],
      leadTimeDays: [null],
      minimumOrderQuantity: [null],
      isPrimarySupplier: [false]
    });
  }

  ngOnInit(): void {
    this.loadSuppliers();
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isEditMode = true;
      this.productId = parseInt(id, 10);
      this.loadProduct(this.productId);
    }
  }

  loadSuppliers(): void {
    this.companyService.getSuppliers().subscribe({
      next: (suppliers) => {
        this.availableSuppliers = suppliers;
      },
      error: (error) => {
        console.error('Error loading suppliers:', error);
      }
    });
  }

  loadProduct(id: number): void {
    this.loading = true;
    this.productService.getById(id).subscribe({
      next: (product) => {
        this.productForm.patchValue({
          name: product.name,
          description: product.description,
          sku: product.sku,
          unitOfMeasurement: product.unitOfMeasurement,
          supplier: product.supplier,
          minimumStockLevel: product.minimumStockLevel,
          initialStock: product.currentStock,
          costPerUnit: product.costPerUnit,
          location: product.location
        });
        this.productSuppliers = product.suppliers || [];
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading product:', error);
        this.errorMessage = 'Failed to load product. Please try again.';
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      Object.keys(this.productForm.controls).forEach(key => {
        this.productForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.saving = true;
    this.errorMessage = '';

    const productData: CreateProductRequest = this.productForm.value;

    if (this.isEditMode && this.productId) {
      this.productService.update(this.productId, productData).subscribe({
        next: () => {
          this.router.navigate(['/products']);
        },
        error: (error: any) => {
          console.error('Error updating product:', error);
          this.errorMessage = error.error?.message || 'Failed to update product. Please try again.';
          this.saving = false;
        }
      });
    } else {
      this.productService.create(productData).subscribe({
        next: (product) => {
          // Navigate to edit mode to add suppliers
          this.router.navigate(['/products/edit', product.id]);
        },
        error: (error: any) => {
          console.error('Error creating product:', error);
          this.errorMessage = error.error?.message || 'Failed to create product. Please try again.';
          this.saving = false;
        }
      });
    }
  }

  toggleSupplierForm(): void {
    this.showSupplierForm = !this.showSupplierForm;
    if (!this.showSupplierForm) {
      this.supplierForm.reset({ isPrimarySupplier: false });
      this.editingSupplier = null;
    }
  }

  addSupplier(): void {
    if (!this.productId || this.supplierForm.invalid) return;

    const supplierData = this.supplierForm.value;

    this.productService.addProductSupplier(this.productId, supplierData).subscribe({
      next: () => {
        this.loadProduct(this.productId!);
        this.toggleSupplierForm();
      },
      error: (error) => {
        console.error('Error adding supplier:', error);
        alert(error.error?.message || 'Failed to add supplier');
      }
    });
  }

  editSupplier(supplier: ProductSupplier): void {
    this.editingSupplier = supplier;
    this.showSupplierForm = true;
    this.supplierForm.patchValue({
      companyId: supplier.companyId,
      supplierPrice: supplier.supplierPrice,
      supplierProductCode: supplier.supplierProductCode,
      leadTimeDays: supplier.leadTimeDays,
      minimumOrderQuantity: supplier.minimumOrderQuantity,
      isPrimarySupplier: supplier.isPrimarySupplier
    });
  }

  updateSupplier(): void {
    if (!this.productId || !this.editingSupplier || this.supplierForm.invalid) return;

    const supplierData = this.supplierForm.value;

    this.productService.updateProductSupplier(this.productId, this.editingSupplier.id, supplierData).subscribe({
      next: () => {
        this.loadProduct(this.productId!);
        this.toggleSupplierForm();
      },
      error: (error) => {
        console.error('Error updating supplier:', error);
        alert(error.error?.message || 'Failed to update supplier');
      }
    });
  }

  deleteSupplier(supplier: ProductSupplier): void {
    if (!this.productId) return;

    if (confirm(`Remove ${supplier.companyName} as a supplier?`)) {
      this.productService.removeProductSupplier(this.productId, supplier.id).subscribe({
        next: () => {
          this.loadProduct(this.productId!);
        },
        error: (error) => {
          console.error('Error removing supplier:', error);
          alert('Failed to remove supplier');
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/products']);
  }

  get name() { return this.productForm.get('name'); }
  get sku() { return this.productForm.get('sku'); }
  get unitOfMeasurement() { return this.productForm.get('unitOfMeasurement'); }
  get minimumStockLevel() { return this.productForm.get('minimumStockLevel'); }
  get initialStock() { return this.productForm.get('initialStock'); }
  get costPerUnit() { return this.productForm.get('costPerUnit'); }
}
