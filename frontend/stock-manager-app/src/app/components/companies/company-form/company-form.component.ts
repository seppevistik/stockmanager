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
import { CompanyService } from '../../../services/company.service';

@Component({
  selector: 'app-company-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule
  ],
  templateUrl: './company-form.component.html',
  styleUrl: './company-form.component.scss'
})
export class CompanyFormComponent implements OnInit {
  companyForm: FormGroup;
  isEditMode = false;
  companyId?: number;
  loading = false;
  saving = false;

  constructor(
    private fb: FormBuilder,
    private companyService: CompanyService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.companyForm = this.fb.group({
      name: ['', Validators.required],
      contactPerson: [''],
      email: ['', [Validators.email]],
      phone: [''],
      address: [''],
      city: [''],
      country: [''],
      postalCode: [''],
      website: [''],
      taxNumber: [''],
      isSupplier: [false],
      isCustomer: [false],
      notes: ['']
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.companyId = +id;
      this.loadCompany();
    }
  }

  loadCompany(): void {
    if (!this.companyId) return;

    this.loading = true;
    this.companyService.getById(this.companyId).subscribe({
      next: (company) => {
        this.companyForm.patchValue(company);
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading company:', error);
        this.loading = false;
        alert('Failed to load company');
        this.cancel();
      }
    });
  }

  onSubmit(): void {
    if (this.companyForm.invalid) return;

    this.saving = true;
    const companyData = this.companyForm.value;

    if (this.isEditMode && this.companyId) {
      this.companyService.update(this.companyId, companyData).subscribe({
        next: () => {
          this.saving = false;
          this.router.navigate(['/companies']);
        },
        error: (error) => {
          console.error('Error updating company:', error);
          this.saving = false;
          alert(error.error?.message || 'Failed to update company');
        }
      });
    } else {
      this.companyService.create(companyData).subscribe({
        next: () => {
          this.saving = false;
          this.router.navigate(['/companies']);
        },
        error: (error) => {
          console.error('Error creating company:', error);
          this.saving = false;
          alert(error.error?.message || 'Failed to create company');
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/companies']);
  }
}
