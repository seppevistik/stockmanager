import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { CustomerService } from '../../../services/customer.service';
import { CreateCustomerDto, UpdateCustomerDto } from '../../../models/customer.model';

@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatSelectModule
  ],
  templateUrl: './customer-form.component.html',
  styleUrl: './customer-form.component.scss'
})
export class CustomerFormComponent implements OnInit {
  customerForm: FormGroup;
  isEditMode = false;
  customerId?: number;
  loading = false;
  saving = false;

  constructor(
    private fb: FormBuilder,
    private customerService: CustomerService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.customerForm = this.fb.group({
      name: ['', Validators.required],
      isCompany: [false],
      contactPerson: [''],
      email: ['', [Validators.email]],
      phone: [''],
      address: [''],
      city: [''],
      state: [''],
      country: [''],
      postalCode: [''],
      companyName: [''],
      taxNumber: [''],
      website: [''],
      creditLimit: [null],
      paymentTermsDays: [null],
      paymentMethod: [''],
      notes: [''],
      isActive: [true]
    });

    // Watch for isCompany changes to adjust validation
    this.customerForm.get('isCompany')?.valueChanges.subscribe(isCompany => {
      if (isCompany) {
        this.customerForm.get('companyName')?.setValidators([Validators.required]);
      } else {
        this.customerForm.get('companyName')?.clearValidators();
      }
      this.customerForm.get('companyName')?.updateValueAndValidity();
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.customerId = +id;
      this.loadCustomer();
    }
  }

  loadCustomer(): void {
    if (!this.customerId) return;

    this.loading = true;
    this.customerService.getCustomerById(this.customerId).subscribe({
      next: (customer) => {
        this.customerForm.patchValue(customer);
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading customer:', error);
        this.loading = false;
        alert('Failed to load customer');
        this.router.navigate(['/customers']);
      }
    });
  }

  onSubmit(): void {
    if (this.customerForm.invalid) {
      Object.keys(this.customerForm.controls).forEach(key => {
        this.customerForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.saving = true;
    const formValue = this.customerForm.value;

    if (this.isEditMode && this.customerId) {
      const updateDto: UpdateCustomerDto = formValue;
      this.customerService.updateCustomer(this.customerId, updateDto).subscribe({
        next: () => {
          this.router.navigate(['/customers']);
        },
        error: (error) => {
          console.error('Error updating customer:', error);
          this.saving = false;
          alert(error.error?.message || 'Failed to update customer');
        }
      });
    } else {
      const createDto: CreateCustomerDto = formValue;
      this.customerService.createCustomer(createDto).subscribe({
        next: () => {
          this.router.navigate(['/customers']);
        },
        error: (error) => {
          console.error('Error creating customer:', error);
          this.saving = false;
          alert(error.error?.message || 'Failed to create customer');
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/customers']);
  }
}
